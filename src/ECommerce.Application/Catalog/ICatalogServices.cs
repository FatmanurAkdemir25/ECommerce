using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Catalog;

public interface ICategoryService
{
    Task<PagedResult<CategoryDto>> ListAsync(CategoryListQuery query, CancellationToken ct);
    Task<CategoryDto> GetAsync(Guid id, CancellationToken ct);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct);
    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public interface IProductService
{
    Task<PagedResult<ProductDto>> ListAsync(ProductListQuery query, CancellationToken ct);
    Task<ProductDto> GetAsync(Guid id, CancellationToken ct);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

// Pozitif Delta = giriş, negatif Delta = çıkış
public record StockChange(Guid ProductId, int Delta);

public interface IStockService
{
    // Tek transaction: ürün stoklarını atomik günceller, 1 fiş + ürün başına 1 satır yazar.
    // Yetersiz stokta ConflictException, ürün yoksa NotFoundException fırlatır.
    // Dış bir transaction varsa ona katılır; bu durumda ürün cache'ini dış transaction
    // commit edildikten sonra çağıranın temizlemesi gerekir.
    Task<Receipt> ApplyAsync(
        ReceiptSource source,
        IReadOnlyList<StockChange> changes,
        Guid? orderId,
        Guid? performedByUserId,
        string? note,
        CancellationToken ct);

    Task<StockAdjustmentResultDto> AdjustAsync(Guid productId, StockAdjustmentRequest request, CancellationToken ct);

    Task<PagedResult<ProductTransactionDto>> ListTransactionsAsync(
        Guid productId, StockHistoryQuery query, CancellationToken ct);
}