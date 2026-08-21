using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(SaviaUpDbContext dbContext) : IOrderRepository
{
    public async Task<Order?> GetActiveByTableIdAsync(Guid tenantId, Guid tableId, CancellationToken cancellationToken)
    {
        return await dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.TableId == tableId && o.Status == "PENDING", cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(Guid tenantId, Guid orderId, CancellationToken cancellationToken)
    {
        return await dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.Id == orderId, cancellationToken);
    }

    public async Task<OrderItem?> GetItemByIdAsync(Guid tenantId, Guid itemId, CancellationToken cancellationToken)
    {
        return await dbContext.OrderItems
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.Order != null && i.Order.TenantId == tenantId && i.Id == itemId, cancellationToken);
    }

    public async Task<int> GetNextOrderNumberAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var max = await dbContext.Orders
            .Where(o => o.TenantId == tenantId)
            .MaxAsync(o => (int?)o.OrderNumber, cancellationToken);
        return (max ?? 0) + 1;
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public async Task AddItemAsync(OrderItem item, CancellationToken cancellationToken)
    {
        await dbContext.OrderItems.AddAsync(item, cancellationToken);
    }

    public void RemoveItem(OrderItem item)
    {
        dbContext.OrderItems.Remove(item);
    }
}
