using AutoMapper;
using AutoMapper.QueryableExtensions;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Accounts;
using ECommerce.Application.Catalog;
using ECommerce.Application.Common;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

namespace ECommerce.Application.Orders;

public class OrderService(
    IAppDbContext db, IStockService stock, ICacheService cache,
    ICurrentUserService currentUser, IMapper mapper, TimeProvider clock) : IOrderService
{
    // ---------- Oluşturma ----------
    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        var userId = await UserGuard.RequireActiveUserAsync(
            currentUser, db, ct);

        var addressOk = await db.Addresses.AsNoTracking()
            .AnyAsync(a => a.Id == request.AddressId && a.UserId == userId, ct);
        if (!addressOk) throw new NotFoundException("Adres bulunamadı.");

        var cart = await db.Carts.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                Items = c.Items.Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    i.Quantity,
                    i.Product.Name,
                    i.Product.Price,
                    i.Product.IsActive,
                    i.Product.Stock
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (cart is null || cart.Items.Count == 0)
            throw new ConflictException("Sepetiniz boş.");

        // Nazik hata mesajları için ön kontrol. Asıl koruma ApplyAsync'teki atomik UPDATE.
        var problems = new List<string>();
        foreach (var i in cart.Items)
        {
            if (!i.IsActive) problems.Add($"'{i.Name}' artık satışta değil.");
            else if (i.Stock < i.Quantity)
                problems.Add($"'{i.Name}' için yeterli stok yok (istenen {i.Quantity}, mevcut {i.Stock}).");
        }
        if (problems.Count > 0) throw new ConflictException(string.Join(" ", problems));

        var total = cart.Items.Sum(i => i.Price * i.Quantity);
        var order = new Order
        {
            UserId = userId,
            AddressId = request.AddressId,
            Status = OrderStatus.Pending,
            TotalAmount = total,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.Price
            }).ToList(),
            Payment = new Payment { Status = PaymentStatus.Pending, Amount = total }
        };

        var changes = cart.Items
            .OrderBy(i => i.ProductId)                       // deadlock önleme
            .Select(i => new StockChange(i.ProductId, -i.Quantity))
            .ToList();

        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            // Çift tıklama koruması: sepeti ilk silen kazanır.
            var ids = cart.Items.Select(i => i.Id).ToList();
            var deleted = await db.CartItems.Where(i => ids.Contains(i.Id)).ExecuteDeleteAsync(ct);
            if (deleted != ids.Count)
                throw new ConflictException("Sepet işlem sırasında değişti. Sepetinizi kontrol edip tekrar deneyin.");

            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);

            await stock.ApplyAsync(ReceiptSource.Order, changes, order.Id, userId, null, ct);
            await tx.CommitAsync(ct);
        }

        await InvalidateProductsAsync(changes);
        return await GetCoreAsync(order.Id, ct);
    }

    // ---------- Sorgular ----------
    public async Task<PagedResult<OrderSummaryDto>> ListMineAsync(OrderListQuery query, CancellationToken ct)
    {
        var userId = await UserGuard.RequireActiveUserAsync(
            currentUser, db, ct);
        return await ListCoreAsync(query, userId, ct);   // query.UserId yok sayılır
    }

    public Task<PagedResult<OrderSummaryDto>> ListAllAsync(OrderListQuery query, CancellationToken ct)
        => ListCoreAsync(query, query.UserId, ct);

    public async Task<OrderDto> GetMineAsync(Guid id, CancellationToken ct)
    {
        var userId = await UserGuard.RequireActiveUserAsync(
            currentUser, db, ct);
        var owned = await db.Orders.AsNoTracking().AnyAsync(o => o.Id == id && o.UserId == userId, ct);
        if (!owned) throw new NotFoundException("Sipariş bulunamadı.");
        return await GetCoreAsync(id, ct);
    }

    public Task<OrderDto> GetAsync(Guid id, CancellationToken ct) => GetCoreAsync(id, ct);

    // ---------- Durum değiştirme ----------
    public async Task<OrderDto> UpdateStatusAsync(Guid id, OrderStatus status, CancellationToken ct)
    {
        if (status == OrderStatus.Cancelled)
            throw new ConflictException("İptal için cancel endpoint'ini kullanın.");

        var order = await db.Orders.AsNoTracking().Where(o => o.Id == id)
            .Select(o => new { o.Status, PaymentStatus = o.Payment != null ? o.Payment.Status : (PaymentStatus?)null })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Sipariş bulunamadı.");

        if (!OrderStatusRules.CanTransition(order.Status, status))
            throw new ConflictException($"Sipariş {order.Status} durumundan {status} durumuna geçirilemez.");

        if (status == OrderStatus.Confirmed && order.PaymentStatus != PaymentStatus.Paid)
            throw new ConflictException("Sipariş onaylanmadan önce ödemenin alınmış olması gerekir.");

        var from = order.Status;
        var updated = await db.Orders.Where(o => o.Id == id && o.Status == from)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, status), ct);
        if (updated == 0)
            throw new ConflictException("Sipariş durumu başka bir işlemle değişti. Tekrar deneyin.");

        return await GetCoreAsync(id, ct);
    }

    public async Task<OrderDto> CancelAsync(Guid id, bool isStaff, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var order = await db.Orders.AsNoTracking().Where(o => o.Id == id)
            .Select(o => new { o.UserId, o.Status }).FirstOrDefaultAsync(ct);

        // Başkasının siparişi: varlığını sızdırmamak için 404
        if (order is null || (!isStaff && order.UserId != userId))
            throw new NotFoundException("Sipariş bulunamadı.");

        if (!OrderStatusRules.CanTransition(order.Status, OrderStatus.Cancelled))
            throw new ConflictException($"{order.Status} durumundaki sipariş iptal edilemez.");

        var from = order.Status;
        var lines = (await db.OrderItems.AsNoTracking().Where(i => i.OrderId == id)
                .Select(i => new { i.ProductId, i.Quantity }).ToListAsync(ct))
            .OrderBy(i => i.ProductId)
            .Select(i => new StockChange(i.ProductId, i.Quantity))   // pozitif = In
            .ToList();

        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            var updated = await db.Orders.Where(o => o.Id == id && o.Status == from)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OrderStatus.Cancelled), ct);
            if (updated == 0)
                throw new ConflictException("Sipariş durumu başka bir işlemle değişti. Tekrar deneyin.");

            await db.Payments.Where(p => p.OrderId == id && p.Status != PaymentStatus.Cancelled)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PaymentStatus.Cancelled), ct);

            await stock.ApplyAsync(ReceiptSource.OrderCancellation, lines, id, userId, "Sipariş iptali", ct);
            await tx.CommitAsync(ct);
        }

        await InvalidateProductsAsync(lines);
        return await GetCoreAsync(id, ct);
    }

    // ---------- Yardımcılar ----------
    private async Task<OrderDto> GetCoreAsync(Guid id, CancellationToken ct)
        => await db.Orders.AsNoTracking().Where(o => o.Id == id)
               .ProjectTo<OrderDto>(mapper.ConfigurationProvider).FirstOrDefaultAsync(ct)
           ?? throw new NotFoundException("Sipariş bulunamadı.");

    private async Task<PagedResult<OrderSummaryDto>> ListCoreAsync(OrderListQuery q, Guid? userId, CancellationToken ct)
    {
        var orders = db.Orders.AsNoTracking().AsQueryable();
        if (userId is not null) orders = orders.Where(o => o.UserId == userId);
        if (q.Status is not null) orders = orders.Where(o => o.Status == q.Status);
        if (q.From is not null) orders = orders.Where(o => o.CreatedAt >= q.From);
        if (q.To is not null) orders = orders.Where(o => o.CreatedAt <= q.To);

        orders = (q.SortBy?.ToLowerInvariant(), q.IsDescending) switch
        {
            ("total", true) => orders.OrderByDescending(o => o.TotalAmount).ThenBy(o => o.Id),
            ("total", false) => orders.OrderBy(o => o.TotalAmount).ThenBy(o => o.Id),
            (_, true) => orders.OrderByDescending(o => o.CreatedAt).ThenBy(o => o.Id),
            _ => orders.OrderBy(o => o.CreatedAt).ThenBy(o => o.Id),
        };

        var total = await orders.CountAsync(ct);
        var items = await orders.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .ProjectTo<OrderSummaryDto>(mapper.ConfigurationProvider).ToListAsync(ct);

        return PagedResult<OrderSummaryDto>.Create(items, total, q);
    }

    private async Task InvalidateProductsAsync(IEnumerable<StockChange> changes)
    {
        // Commit olduktan sonra çalışır; istek iptal edilse bile cache bayat kalmasın
        foreach (var c in changes)
            await cache.RemoveAsync(CacheKeys.Product(c.ProductId), CancellationToken.None);
    }
}