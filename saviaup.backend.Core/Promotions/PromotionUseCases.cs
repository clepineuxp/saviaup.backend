using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Promotions;

internal static class PromotionRules
{
    public static string Normalize(string value) => string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

    public static bool TryParse(string type, string valueType, out PromotionType parsedType, out PromotionValueType parsedValueType)
    {
        var typeParsed = Enum.TryParse(type.Trim(), true, out PromotionType parsedTypeValue);
        var valueParsed = Enum.TryParse(valueType.Trim(), true, out PromotionValueType parsedValueTypeValue);
        parsedType = parsedTypeValue;
        parsedValueType = parsedValueTypeValue;
        return typeParsed && valueParsed;
    }

    public static bool IsValid(CreatePromotionRequest request, out PromotionType type, out PromotionValueType valueType)
    {
        type = default;
        valueType = default;
        var normalizedName = Normalize(request.Name ?? string.Empty);
        if (!TryParse(request.Type, request.ValueType, out type, out valueType)
            || normalizedName.Length is 0 or > 120
            || request.Description?.Length > 1000
            || request.Value <= 0
            || (valueType == PromotionValueType.Percentage && request.Value > 100)
            || (request.StartTime.HasValue != request.EndTime.HasValue)
            || (request.StartTime.HasValue && request.StartTime == request.EndTime)
            || request.WeekDaysMask is < 0 or > 127
            || request.Targets is null or { Count: 0 })
            return false;

        if (type == PromotionType.Quantity
            && (request.RequiredQuantity is null
                || request.RequiredQuantity < 2
                || request.DiscountedQuantity is null
                || request.DiscountedQuantity < 1
                || request.DiscountedQuantity >= request.RequiredQuantity))
            return false;

        if (type == PromotionType.General
            && (request.RequiredQuantity is not null || request.DiscountedQuantity is not null))
            return false;

        return request.Targets.All(IsValidTarget);
    }

    private static bool IsValidTarget(PromotionTargetRequest target)
    {
        if (!Enum.TryParse<PromotionTargetType>(target.TargetType.Trim(), true, out var targetType)) return false;
        return targetType switch
        {
            PromotionTargetType.Menu => target.ProductId is null && target.ProductVariationId is null && target.CategoryId is null,
            PromotionTargetType.Product => target.ProductId.HasValue && target.ProductVariationId is null && target.CategoryId is null,
            PromotionTargetType.Variation => target.ProductId.HasValue && target.ProductVariationId.HasValue && target.CategoryId is null,
            PromotionTargetType.Category => target.CategoryId.HasValue && target.ProductId is null && target.ProductVariationId is null,
            _ => false
        };
    }

    public static PromotionDto ToDto(Promotion promotion)
        => new(
            promotion.Id,
            promotion.Name,
            promotion.Description,
            promotion.Type.ToString().ToUpperInvariant(),
            promotion.ValueType.ToString().ToUpperInvariant(),
            promotion.Value,
            promotion.RequiredQuantity,
            promotion.DiscountedQuantity,
            promotion.StartTime,
            promotion.EndTime,
            promotion.SpecificDate,
            promotion.MonthOfYear,
            promotion.WeekDaysMask,
            promotion.IsActive,
            promotion.CreatedAt,
            promotion.UpdatedAt,
            promotion.Targets.Select(target => new PromotionTargetDto(
                target.TargetType.ToString().ToUpperInvariant(),
                target.ProductId,
                target.Product?.Name,
                target.ProductVariationId,
                target.ProductVariation?.Name,
                target.CategoryId,
                target.Category?.Name)).ToArray());

    public static PromotionTarget[] BuildTargets(Guid tenantId, Guid promotionId, IReadOnlyCollection<PromotionTargetRequest> requests)
        => requests.Select(request => new PromotionTarget
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PromotionId = promotionId,
            TargetType = Enum.Parse<PromotionTargetType>(request.TargetType.Trim(), true),
            ProductId = request.ProductId,
            ProductVariationId = request.ProductVariationId,
            CategoryId = request.CategoryId
        }).ToArray();
}

public abstract class PromotionWriteUseCaseBase
{
    protected PromotionWriteUseCaseBase(
        IPromotionRepository repository,
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        PromotionRepository = repository;
        ProductRepository = productRepository;
        CategoryRepository = categoryRepository;
        UnitOfWork = unitOfWork;
        Clock = clock;
    }

    protected IPromotionRepository PromotionRepository { get; }
    protected IProductRepository ProductRepository { get; }
    protected ICategoryRepository CategoryRepository { get; }
    protected IUnitOfWork UnitOfWork { get; }
    protected IDateTimeProvider Clock { get; }

    protected async Task<bool> ValidateTargetsAsync(
        Guid tenantId,
        IReadOnlyCollection<PromotionTargetRequest> requests,
        CancellationToken cancellationToken)
    {
        var products = await ProductRepository.GetAllForTenantAsync(tenantId, true, cancellationToken);
        var categories = await CategoryRepository.GetForTenantAsync(tenantId, true, cancellationToken);
        var productIds = products.Select(x => x.Id).ToHashSet();
        var variations = products.SelectMany(x => x.Variations).ToDictionary(x => x.Id, x => x);
        var categoryIds = categories.Select(x => x.Id).ToHashSet();

        return requests.All(request =>
        {
            if (!Enum.TryParse<PromotionTargetType>(request.TargetType.Trim(), true, out var targetType)) return false;
            return targetType switch
            {
                PromotionTargetType.Menu => request.ProductId is null && request.ProductVariationId is null && request.CategoryId is null,
                PromotionTargetType.Product => request.ProductId is { } productId && productIds.Contains(productId) && request.ProductVariationId is null && request.CategoryId is null,
                PromotionTargetType.Variation => request.ProductId is { } productId
                    && request.ProductVariationId is { } variationId
                    && productIds.Contains(productId)
                    && variations.TryGetValue(variationId, out var variation)
                    && variation.ProductId == productId
                    && request.CategoryId is null,
                PromotionTargetType.Category => request.CategoryId is { } categoryId && categoryIds.Contains(categoryId) && request.ProductId is null && request.ProductVariationId is null,
                _ => false
            };
        });
    }

    protected async Task<Result<PromotionDto>> SaveAsync(
        Guid tenantId,
        Promotion promotion,
        IReadOnlyCollection<PromotionTargetRequest> targets,
        CancellationToken cancellationToken)
    {
        promotion.Targets.Clear();
        foreach (var target in PromotionRules.BuildTargets(tenantId, promotion.Id, targets)) promotion.Targets.Add(target);
        promotion.UpdatedAt = Clock.UtcNow;
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        var reloaded = await PromotionRepository.GetByIdAsync(tenantId, promotion.Id, cancellationToken);
        return Result<PromotionDto>.Success(PromotionRules.ToDto(reloaded ?? promotion));
    }
}

public sealed class ListPromotionsUseCase(IPromotionRepository repository) : IListPromotionsUseCase
{
    public async Task<Result<IReadOnlyCollection<PromotionDto>>> ExecuteAsync(Guid tenantId, bool includeInactive, CancellationToken cancellationToken)
        => Result<IReadOnlyCollection<PromotionDto>>.Success(
            (await repository.GetForTenantAsync(tenantId, includeInactive, cancellationToken)).Select(PromotionRules.ToDto).ToArray());
}

public sealed class CreatePromotionUseCase(
    IPromotionRepository repository,
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : PromotionWriteUseCaseBase(repository, productRepository, categoryRepository, unitOfWork, clock), ICreatePromotionUseCase
{
    public async Task<Result<PromotionDto>> ExecuteAsync(Guid tenantId, CreatePromotionRequest request, CancellationToken cancellationToken)
    {
        if (!PromotionRules.IsValid(request, out var type, out var valueType)
            || !await ValidateTargetsAsync(tenantId, request.Targets, cancellationToken))
            return Result<PromotionDto>.Failure(Errors.Validation);

        var normalizedName = PromotionRules.Normalize(request.Name);
        var existing = await PromotionRepository.GetForTenantAsync(tenantId, true, cancellationToken);
        if (existing.Any(x => x.NormalizedName == normalizedName)) return Result<PromotionDto>.Failure(Errors.PromotionNameAlreadyExists);

        var now = Clock.UtcNow;
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Name = request.Name.Trim(), NormalizedName = normalizedName,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(), Type = type,
            ValueType = valueType, Value = request.Value, RequiredQuantity = request.RequiredQuantity,
            DiscountedQuantity = request.DiscountedQuantity, StartTime = request.StartTime, EndTime = request.EndTime,
            SpecificDate = request.SpecificDate, MonthOfYear = request.MonthOfYear, WeekDaysMask = request.WeekDaysMask,
            IsActive = request.IsActive, CreatedAt = now, UpdatedAt = now
        };
        foreach (var target in PromotionRules.BuildTargets(tenantId, promotion.Id, request.Targets)) promotion.Targets.Add(target);
        await PromotionRepository.AddAsync(promotion, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        var created = await PromotionRepository.GetByIdAsync(tenantId, promotion.Id, cancellationToken);
        return Result<PromotionDto>.Success(PromotionRules.ToDto(created ?? promotion));
    }
}

public sealed class UpdatePromotionUseCase(
    IPromotionRepository repository,
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : PromotionWriteUseCaseBase(repository, productRepository, categoryRepository, unitOfWork, clock), IUpdatePromotionUseCase
{
    public async Task<Result<PromotionDto>> ExecuteAsync(Guid tenantId, Guid promotionId, UpdatePromotionRequest request, CancellationToken cancellationToken)
    {
        if (!PromotionRules.IsValid(new CreatePromotionRequest(request.Name, request.Description, request.Type, request.ValueType, request.Value, request.RequiredQuantity, request.DiscountedQuantity, request.StartTime, request.EndTime, request.SpecificDate, request.MonthOfYear, request.WeekDaysMask, request.Targets, request.IsActive), out var type, out var valueType)
            || !await ValidateTargetsAsync(tenantId, request.Targets, cancellationToken))
            return Result<PromotionDto>.Failure(Errors.Validation);

        var promotion = await PromotionRepository.GetByIdAsync(tenantId, promotionId, cancellationToken);
        if (promotion is null) return Result<PromotionDto>.Failure(Errors.PromotionNotFound);
        var normalizedName = PromotionRules.Normalize(request.Name);
        var existing = await PromotionRepository.GetForTenantAsync(tenantId, true, cancellationToken);
        if (existing.Any(x => x.Id != promotionId && x.NormalizedName == normalizedName)) return Result<PromotionDto>.Failure(Errors.PromotionNameAlreadyExists);

        promotion.Name = request.Name.Trim(); promotion.NormalizedName = normalizedName;
        promotion.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        promotion.Type = type; promotion.ValueType = valueType; promotion.Value = request.Value;
        promotion.RequiredQuantity = request.RequiredQuantity; promotion.DiscountedQuantity = request.DiscountedQuantity;
        promotion.StartTime = request.StartTime; promotion.EndTime = request.EndTime; promotion.SpecificDate = request.SpecificDate;
        promotion.MonthOfYear = request.MonthOfYear; promotion.WeekDaysMask = request.WeekDaysMask; promotion.IsActive = request.IsActive;
        return await SaveAsync(tenantId, promotion, request.Targets, cancellationToken);
    }
}

public sealed class SetPromotionStatusUseCase(IPromotionRepository repository, IUnitOfWork unitOfWork, IDateTimeProvider clock) : ISetPromotionStatusUseCase
{
    public async Task<Result<PromotionDto>> ExecuteAsync(Guid tenantId, Guid promotionId, SetPromotionStatusRequest request, CancellationToken cancellationToken)
    {
        var promotion = await repository.GetByIdAsync(tenantId, promotionId, cancellationToken);
        if (promotion is null) return Result<PromotionDto>.Failure(Errors.PromotionNotFound);
        promotion.IsActive = request.IsActive; promotion.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<PromotionDto>.Success(PromotionRules.ToDto(promotion));
    }
}

public sealed class DeletePromotionUseCase(IPromotionRepository repository, IUnitOfWork unitOfWork) : IDeletePromotionUseCase
{
    public async Task<Result> ExecuteAsync(Guid tenantId, Guid promotionId, CancellationToken cancellationToken)
    {
        var promotion = await repository.GetByIdAsync(tenantId, promotionId, cancellationToken);
        if (promotion is null) return Result.Failure(Errors.PromotionNotFound);
        repository.Remove(promotion);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class PromotionPricingService(
    IPromotionRepository repository,
    IOrganizationTimeZone organizationTimeZone,
    ITimeZoneService timeZoneService) : IPromotionPricingService
{
    public async Task<IReadOnlyCollection<Promotion>> GetApplicableAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var promotions = await repository.GetActiveForEvaluationAsync(tenantId, cancellationToken);
        var timeZone = await organizationTimeZone.GetAsync(tenantId, cancellationToken);
        var local = timeZoneService.ConvertFromUtc(now, timeZone);
        var date = DateOnly.FromDateTime(local.DateTime);
        var time = TimeOnly.FromDateTime(local.DateTime);
        var weekdayBit = 1 << (int)date.DayOfWeek;
        return promotions.Where(x =>
            (!x.SpecificDate.HasValue || x.SpecificDate == date)
            && (!x.MonthOfYear.HasValue || x.MonthOfYear == date.Month)
            && (x.WeekDaysMask == 0 || (x.WeekDaysMask & weekdayBit) != 0)
            && (!x.StartTime.HasValue || !x.EndTime.HasValue || time >= x.StartTime && time <= x.EndTime)).ToArray();
    }

    public PromotionCalculation? FindBest(IReadOnlyCollection<Promotion> promotions, Product product, Guid? variationId, decimal originalUnitPrice, int quantity)
    {
        if (originalUnitPrice <= 0 || quantity <= 0) return null;
        return promotions
            .Where(x => x.Targets.Any(target => target.TargetType == PromotionTargetType.Menu
                || target.TargetType == PromotionTargetType.Product && target.ProductId == product.Id
                || target.TargetType == PromotionTargetType.Category && target.CategoryId == product.CategoryId
                || target.TargetType == PromotionTargetType.Variation && target.ProductId == product.Id && target.ProductVariationId == variationId))
            .Select(x => Calculate(x, originalUnitPrice, quantity))
            .Where(x => x is not null)
            .Cast<PromotionCalculation>()
            .OrderByDescending(x => x.DiscountAmount)
            .ThenBy(x => x.FinalUnitPrice)
            .FirstOrDefault();
    }

    private static PromotionCalculation? Calculate(Promotion promotion, decimal originalUnitPrice, int quantity)
    {
        var discountedUnits = promotion.Type == PromotionType.General
            ? quantity
            : quantity / promotion.RequiredQuantity!.Value * promotion.DiscountedQuantity!.Value;
        if (discountedUnits <= 0) return null;
        var discountPerUnit = promotion.ValueType == PromotionValueType.Percentage
            ? originalUnitPrice * promotion.Value / 100m
            : Math.Max(0m, originalUnitPrice - promotion.Value);
        var requestedDiscount = Math.Round(Math.Min(originalUnitPrice * discountedUnits, discountPerUnit * discountedUnits), 2, MidpointRounding.AwayFromZero);
        var finalUnitPrice = Math.Round(originalUnitPrice - requestedDiscount / quantity, 2, MidpointRounding.AwayFromZero);
        var appliedDiscount = Math.Round(originalUnitPrice * quantity - finalUnitPrice * quantity, 2, MidpointRounding.AwayFromZero);
        return new PromotionCalculation(promotion, originalUnitPrice, Math.Max(0m, finalUnitPrice), Math.Max(0m, appliedDiscount));
    }
}
