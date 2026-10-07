using AutoMapper;
using AutoMapper.QueryableExtensions;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Constants;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Catalog;

public class ProductService(
    IAppDbContext db,
    IMapper mapper,
    ICacheService cache,
    ICurrentUserService currentUser,
    CatalogVisibility visibility,
    ILogger<ProductService> logger) : IProductService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<PagedResult<ProductDto>> ListAsync(ProductListQuery query, CancellationToken ct)
    {
        var products = db.Products.AsNoTracking();

        if (!await visibility.CanSeeInactiveAsync(Permissions.Products.Update, ct))
        {
            products = products.Where(p => p.IsActive);
        }
        else if (query.IsActive is { } active)
        {
            products = products.Where(p => p.IsActive == active);
        }

        if (query.CategoryId is { } categoryId) products = products.Where(p => p.CategoryId == categoryId);
        if (query.MinPrice is { } min) products = products.Where(p => p.Price >= min);
        if (query.MaxPrice is { } max) products = products.Where(p => p.Price <= max);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            products = products.Where(p => p.Name.Contains(search));
        }

        var total = await products.CountAsync(ct);

        // Sayfalar arasında kayıt atlanmasın / tekrarlanmasın diye ikincil sıralama Id
        var ordered = (query.SortBy?.ToLowerInvariant(), query.IsDescending) switch
        {
            ("price", false) => products.OrderBy(p => p.Price).ThenBy(p => p.Id),
            ("price", true) => products.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
            ("date" or "createdat", false) => products.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),
            ("date" or "createdat", true) => products.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
            (_, true) => products.OrderByDescending(p => p.Name).ThenBy(p => p.Id),
            _ => products.OrderBy(p => p.Name).ThenBy(p => p.Id)
        };

        var items = await ordered.Skip(query.Skip).Take(query.PageSize)
            .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return PagedResult<ProductDto>.Create(items, total, query);
    }

    public async Task<ProductDto> GetAsync(Guid id, CancellationToken ct)
    {
        // Cache'te pasif ürün de olabilir; görünürlük her istekte cache'ten sonra kontrol edilir
        var product = await cache.GetOrCreateAsync<ProductDto?>(
            CacheKeys.Product(id), c => LoadAsync(id, c), CacheDuration, ct);

        if (product is null ||
            (!product.IsActive && !await visibility.CanSeeInactiveAsync(Permissions.Products.Update, ct)))
        {
            throw new NotFoundException("Ürün", id);
        }

        return product;
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("Kimlik doğrulanamadı.");
        await EnsureCategoryExistsAsync(request.CategoryId, ct);

        var product = new Product
        {
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Price = request.Price,
            Stock = request.Stock,
            IsActive = request.IsActive
        };
        db.Products.Add(product);

        if (request.Stock > 0)
        {
            var receipt = new Receipt
            {
                Source = ReceiptSource.InitialStock,
                PerformedByUserId = userId,
                Note = "İlk stok"
            };
            receipt.Transactions.Add(new ProductTransaction
            {
                ProductId = product.Id,
                Type = ProductTransactionType.In,
                Quantity = request.Stock,
                StockAfter = request.Stock
            });
            db.Receipts.Add(receipt);
        }

        // Ürün, fiş ve satır tek SaveChanges içinde, yani tek transaction'da yazılır
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Ürün oluşturuldu. ProductId: {ProductId}", product.Id);
        return (await LoadAsync(product.Id, ct))!;
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Ürün", id);

        if (product.CategoryId != request.CategoryId)
        {
            await EnsureCategoryExistsAsync(request.CategoryId, ct);
        }

        // Stok burada değişmez: sadece IStockService üzerinden değişir
        product.CategoryId = request.CategoryId;
        product.Name = request.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        product.Price = request.Price;
        product.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);

        await cache.RemoveAsync(CacheKeys.Product(id), ct);
        logger.LogInformation("Ürün güncellendi. ProductId: {ProductId}", id);
        return (await LoadAsync(id, ct))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Ürün", id);

        var hasHistory = await db.OrderItems.AnyAsync(oi => oi.ProductId == id, ct)
            || await db.ProductTransactions.AnyAsync(t => t.ProductId == id, ct);
        if (hasHistory)
        {
            throw new ConflictException("Siparişi veya stok geçmişi olan ürün silinemez. Ürünü pasife alın.");
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);

        await cache.RemoveAsync(CacheKeys.Product(id), ct);
        logger.LogInformation("Ürün silindi. ProductId: {ProductId}", id);
    }

    private Task<ProductDto?> LoadAsync(Guid id, CancellationToken ct) =>
        db.Products.AsNoTracking()
            .Where(p => p.Id == id)
            .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);

    private async Task EnsureCategoryExistsAsync(Guid categoryId, CancellationToken ct)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == categoryId, ct))
        {
            throw new NotFoundException("Kategori", categoryId);
        }
    }
}