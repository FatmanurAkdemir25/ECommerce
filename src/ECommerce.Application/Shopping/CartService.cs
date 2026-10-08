using AutoMapper;
using AutoMapper.QueryableExtensions;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Accounts;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Shopping;

public class CartService(
    IAppDbContext db,
    IMapper mapper,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    ILogger<CartService> logger) : ICartService
{
    private const int MaxAttempts = 3;

    private sealed record ProductSnapshot(Guid Id, bool IsActive, int Stock);

    public async Task<CartDto> GetAsync(CancellationToken ct)
    {
        var userId = await UserGuard.RequireActiveUserAsync(currentUser, db, ct);
        return await LoadCartAsync(userId, ct);
    }

    public Task<CartDto> AddItemAsync(AddCartItemRequest request, CancellationToken ct) =>
        MutateAsync(async userId =>
        {
            var product = await LoadSellableProductAsync(request.ProductId, ct);
            var cart = await GetOrCreateCartAsync(userId, ct);

            var item = await db.CartItems.FirstOrDefaultAsync(
                i => i.CartId == cart.Id && i.ProductId == product.Id, ct);

            // Stok kontrolü sepette zaten olan miktarı da sayar
            var quantity = (item?.Quantity ?? 0) + request.Quantity;
            EnsureQuantityAllowed(product, quantity);

            if (item is null)
            {
                var distinct = await db.CartItems.CountAsync(i => i.CartId == cart.Id, ct);
                if (distinct >= CartLimits.MaxDistinctItems)
                {
                    throw new ConflictException(
                        $"Sepette en fazla {CartLimits.MaxDistinctItems} farklı ürün bulunabilir.");
                }

                db.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = product.Id, Quantity = quantity });
            }
            else
            {
                item.Quantity = quantity;
            }

            cart.UpdatedAt = Now();

            // Yeni sepet (varsa) ve kalem tek SaveChanges içinde, yani tek transaction'da yazılır
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Sepete ürün eklendi. UserId: {UserId}, ProductId: {ProductId}", userId, product.Id);
        }, ct);

    public Task<CartDto> SetQuantityAsync(Guid productId, SetCartItemQuantityRequest request, CancellationToken ct) =>
        MutateAsync(async userId =>
        {
            var cart = await db.Carts.FirstOrDefaultAsync(c => c.UserId == userId, ct)
                ?? throw new NotFoundException("Sepet kalemi", productId);

            var item = await db.CartItems.FirstOrDefaultAsync(
                i => i.CartId == cart.Id && i.ProductId == productId, ct)
                ?? throw new NotFoundException("Sepet kalemi", productId);

            var product = await LoadSellableProductAsync(productId, ct);
            EnsureQuantityAllowed(product, request.Quantity);

            item.Quantity = request.Quantity;
            cart.UpdatedAt = Now();
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Sepet miktarı güncellendi. UserId: {UserId}, ProductId: {ProductId}", userId, productId);
        }, ct);

    public async Task RemoveItemAsync(Guid productId, CancellationToken ct)
    {
        var userId = await UserGuard.RequireActiveUserAsync(currentUser, db, ct);

        await WithRetryAsync(async () =>
        {
            var cart = await db.Carts.FirstOrDefaultAsync(c => c.UserId == userId, ct);
            if (cart is null) return; // idempotent

            var item = await db.CartItems.FirstOrDefaultAsync(
                i => i.CartId == cart.Id && i.ProductId == productId, ct);
            if (item is null) return; // idempotent

            db.CartItems.Remove(item);
            cart.UpdatedAt = Now();
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Sepetten ürün çıkarıldı. UserId: {UserId}, ProductId: {ProductId}", userId, productId);
        });
    }

    public async Task ClearAsync(CancellationToken ct)
    {
        var userId = await UserGuard.RequireActiveUserAsync(currentUser, db, ct);

        await WithRetryAsync(async () =>
        {
            var cart = await db.Carts.FirstOrDefaultAsync(c => c.UserId == userId, ct);
            if (cart is null) return; // idempotent

            var items = await db.CartItems.Where(i => i.CartId == cart.Id).ToListAsync(ct);
            db.CartItems.RemoveRange(items);
            cart.UpdatedAt = Now();
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Sepet temizlendi. UserId: {UserId}", userId);
        });
    }

    private async Task<CartDto> MutateAsync(Func<Guid, Task> mutation, CancellationToken ct)
    {
        var userId = await UserGuard.RequireActiveUserAsync(currentUser, db, ct);
        await WithRetryAsync(() => mutation(userId));
        return await LoadCartAsync(userId, ct);
    }

    // Aynı kullanıcının eş zamanlı isteklerinde (çift tıklama, iki sekme) sepet veya kalem
    // benzersizlik çakışması oluşabilir. Değişiklikler atılıp işlem yeniden denenir.
    private async Task WithRetryAsync(Func<Task> operation)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await operation();
                return;
            }
            catch (DbUpdateException) when (attempt < MaxAttempts)
            {
                db.ChangeTracker.Clear();
            }
        }
    }

    private async Task<CartDto> LoadCartAsync(Guid userId, CancellationToken ct)
    {
        var cart = await db.Carts.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new { c.Id, c.UpdatedAt })
            .FirstOrDefaultAsync(ct);

        if (cart is null) return new CartDto(); // sepet yok: boş sepet, veritabanına yazılmaz

        var items = await db.CartItems.AsNoTracking()
            .Where(i => i.CartId == cart.Id)
            .OrderBy(i => i.Product.Name).ThenBy(i => i.ProductId)
            .ProjectTo<CartItemDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        foreach (var item in items)
        {
            item.Issue = !item.IsProductActive
                ? "Ürün artık satışta değil."
                : item.Quantity > item.AvailableStock
                    ? $"Stok yetersiz (mevcut: {item.AvailableStock})."
                    : null;
        }

        return new CartDto
        {
            UpdatedAt = cart.UpdatedAt,
            Items = items,
            TotalQuantity = items.Sum(i => i.Quantity),
            TotalAmount = items.Sum(i => i.LineTotal),
            HasIssues = items.Any(i => i.Issue is not null)
        };
    }

    // Sepet yoksa oluşturulur ama burada kaydedilmez; çağıranın SaveChanges'iyle birlikte yazılır
    private async Task<Cart> GetOrCreateCartAsync(Guid userId, CancellationToken ct)
    {
        var cart = await db.Carts.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (cart is not null) return cart;

        cart = new Cart { UserId = userId, UpdatedAt = Now() };
        db.Carts.Add(cart);
        return cart;
    }

    private async Task<ProductSnapshot> LoadSellableProductAsync(Guid productId, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new ProductSnapshot(p.Id, p.IsActive, p.Stock))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Ürün", productId);

        if (!product.IsActive)
        {
            throw new ConflictException("Bu ürün şu anda satışta değil.");
        }

        return product;
    }

    private static void EnsureQuantityAllowed(ProductSnapshot product, int quantity)
    {
        if (quantity > product.Stock)
        {
            throw new ConflictException(
                $"Yetersiz stok. Sepetteki toplam miktar {quantity} olacak, mevcut stok: {product.Stock}.");
        }

        if (quantity > CartLimits.MaxQuantityPerItem)
        {
            throw new ConflictException($"Bir üründen en fazla {CartLimits.MaxQuantityPerItem} adet alabilirsiniz.");
        }
    }

    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;
}