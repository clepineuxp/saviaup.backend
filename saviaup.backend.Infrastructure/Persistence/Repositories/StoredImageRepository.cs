using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class StoredImageRepository(ApplicationDbContext context) : IStoredImageRepository
{
    public async Task AddAsync(StoredImage image, CancellationToken cancellationToken)
    {
        await context.StoredImages.AddAsync(image, cancellationToken);
    }

    public async Task<StoredImage?> GetByIdAsync(Guid tenantId, Guid imageId, CancellationToken cancellationToken)
    {
        return await context.StoredImages
            .FirstOrDefaultAsync(x => x.Id == imageId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<StoredImage>> GetByEntityAsync(Guid tenantId, string module, string entityId, CancellationToken cancellationToken)
    {
        return await context.StoredImages
            .Where(x => x.Module == module && x.EntityId == entityId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void Remove(StoredImage image)
    {
        context.StoredImages.Remove(image);
    }
}
