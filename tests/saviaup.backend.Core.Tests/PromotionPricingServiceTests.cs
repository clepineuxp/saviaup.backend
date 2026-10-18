using Moq;
using SaviaUp.Backend.Core.Promotions;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using Xunit;

namespace SaviaUp.Backend.Core.Tests;

public sealed class PromotionPricingServiceTests
{
    [Fact]
    public void AppliesPercentagePromotionToProduct()
    {
        var product = Product();
        var promotion = Promotion(PromotionType.General, PromotionValueType.Percentage, 20m, PromotionTargetType.Product, product.Id);
        var service = Service();

        var result = service.FindBest([promotion], product, null, 50_000m, 1);

        Assert.NotNull(result);
        Assert.Equal(40_000m, result.FinalUnitPrice);
        Assert.Equal(10_000m, result.DiscountAmount);
    }

    [Fact]
    public void AppliesTwoForOneOnlyToCompleteQuantityBlocks()
    {
        var product = Product();
        var promotion = Promotion(PromotionType.Quantity, PromotionValueType.Percentage, 100m, PromotionTargetType.Product, product.Id);
        promotion.RequiredQuantity = 2;
        promotion.DiscountedQuantity = 1;
        var service = Service();

        var result = service.FindBest([promotion], product, null, 10_000m, 3);

        Assert.NotNull(result);
        Assert.Equal(6_666.67m, result.FinalUnitPrice);
        Assert.Equal(9_999.99m, result.DiscountAmount);
    }

    [Fact]
    public void ChoosesThePromotionWithTheLargestDiscount()
    {
        var product = Product();
        var ten = Promotion(PromotionType.General, PromotionValueType.Percentage, 10m, PromotionTargetType.Product, product.Id);
        var twenty = Promotion(PromotionType.General, PromotionValueType.Percentage, 20m, PromotionTargetType.Category, product.CategoryId);
        var service = Service();

        var result = service.FindBest([ten, twenty], product, null, 50_000m, 1);

        Assert.NotNull(result);
        Assert.Equal(twenty.Id, result.Promotion.Id);
    }

    private static PromotionPricingService Service()
        => new(
            Mock.Of<IPromotionRepository>(),
            Mock.Of<IOrganizationTimeZone>(),
            Mock.Of<ITimeZoneService>());

    private static Product Product()
        => new() { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Name = "Cóctel", SalePrice = 50_000m };

    private static Promotion Promotion(
        PromotionType type,
        PromotionValueType valueType,
        decimal value,
        PromotionTargetType targetType,
        Guid targetId)
    {
        Guid? productId = targetType is PromotionTargetType.Product or PromotionTargetType.Category ? targetId : null;
        return new()
        {
            Id = Guid.NewGuid(),
            Name = "Promo",
            Type = type,
            ValueType = valueType,
            Value = value,
            Targets =
            [
                new PromotionTarget
                {
                    TargetType = targetType,
                    ProductId = targetType == PromotionTargetType.Product ? targetId : productId,
                    CategoryId = targetType == PromotionTargetType.Category ? targetId : null
                }
            ]
        };
    }
}
