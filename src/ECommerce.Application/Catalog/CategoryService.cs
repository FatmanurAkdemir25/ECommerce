using AutoMapper;
using AutoMapper.QueryableExtensions;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Constants;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Catalog;

public class CategoryService(
    IAppDbContext db,
    IMapper mapper,
    ICacheService cache,
    CatalogVisibility visibility,
    ILogger<CategoryService> logger) : ICategoryService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public async Task<PagedResult<CategoryDto>> ListAsync(CategoryListQuery query, CancellationToken ct)
    {
        // Kategori sayısı az olduğu için tüm liste cache'lenir; filtre, sıralama ve sayfalama bellekte yapılır
        IEnumerable<CategoryDto> items = await GetAllAsync(ct);

        if (!await visibility.CanSeeInactiveAsync(Permissions.Categories.Update, ct))
        {
            items = items.Where(c => c.IsActive);
        }
        else if (query.IsActive is { } active)
        {
            items = items.Where(c => c.IsActive == active);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            items = items.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        items = query.IsDescending
            ? items.OrderByDescending(c => c.Name, StringComparer.OrdinalIgnoreCase)
            : items.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase);

        var filtered = items.ToList();
        var page = filtered.Skip(query.Skip).Take(query.PageSize).ToList();
        return PagedResult<CategoryDto>.Create(page, filtered.Count, query);
    }

    public async Task<CategoryDto> GetAsync(Guid id, CancellationToken ct)
    {
        var category = (await GetAllAsync(ct)).FirstOrDefault(c => c.Id == id);

        if (category is null ||
            (!category.IsActive && !await visibility.CanSeeInactiveAsync(Permissions.Categories.Update, ct)))
        {
            throw new NotFoundException("Kategori", id);
        }

        return category;
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (await db.Categories.AnyAsync(c => c.Name == name, ct))
        {
            throw new ConflictException($"'{name}' adında bir kategori zaten var.");
        }

        var category = new Category { Name = name, IsActive = request.IsActive };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        await cache.RemoveAsync(CacheKeys.AllCategories, ct);
        logger.LogInformation("Kategori oluşturuldu. CategoryId: {CategoryId}", category.Id);
        return mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Kategori", id);

        var name = request.Name.Trim();
        if (await db.Categories.AnyAsync(c => c.Id != id && c.Name == name, ct))
        {
            throw new ConflictException($"'{name}' adında bir kategori zaten var.");
        }

        var nameChanged = category.Name != name;
        category.Name = name;
        category.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);

        await cache.RemoveAsync(CacheKeys.AllCategories, ct);

        // Ürün detaylarında kategori adı da var, ad değiştiyse o ürünlerin cache'i temizlenir
        if (nameChanged)
        {
            var productIds = await db.Products.AsNoTracking()
                .Where(p => p.CategoryId == id)
                .Select(p => p.Id)
                .ToListAsync(ct);

            foreach (var productId in productIds)
            {
                await cache.RemoveAsync(CacheKeys.Product(productId), ct);
            }
        }

        logger.LogInformation("Kategori güncellendi. CategoryId: {CategoryId}", id);
        return mapper.Map<CategoryDto>(category);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Kategori", id);

        var productCount = await db.Products.CountAsync(p => p.CategoryId == id, ct);
        if (productCount > 0)
        {
            throw new ConflictException(
                $"Bu kategoride {productCount} ürün var. Önce ürünleri başka kategoriye taşıyın veya kategoriyi pasife alın.");
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);

        await cache.RemoveAsync(CacheKeys.AllCategories, ct);
        logger.LogInformation("Kategori silindi. CategoryId: {CategoryId}", id);
    }

    private Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct) =>
        cache.GetOrCreateAsync<IReadOnlyList<CategoryDto>>(
            CacheKeys.AllCategories,
            async c => await db.Categories.AsNoTracking()
                .OrderBy(x => x.Name)
                .ProjectTo<CategoryDto>(mapper.ConfigurationProvider)
                .ToListAsync(c),
            CacheDuration,
            ct);
}