using ECommerce.Application.Common.Models;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Catalog;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

// SortBy şimdilik sadece "name" destekler
public class CategoryListQuery : PagedQuery
{
    public string? Search { get; set; }

    // Sadece personel için geçerli; diğerleri her zaman sadece aktifleri görür
    public bool? IsActive { get; set; }
}

public class ProductDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductRequest
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }

    // İlk stok. Sıfırdan büyükse "InitialStock" fişi yazılır
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
}

// Stok yok: stok sadece stok endpoint'iyle değişir
public class UpdateProductRequest
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}

// SortBy: price | name | date
public class ProductListQuery : PagedQuery
{
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    // Sadece personel için geçerli
    public bool? IsActive { get; set; }
}

public class StockAdjustmentRequest
{
    // Zorunlu: boş bırakılırsa 400 döner (varsayılan "giriş" olmasın)
    public ProductTransactionType? Type { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
}

public class StockAdjustmentResultDto
{
    public Guid ProductId { get; set; }
    public Guid ReceiptId { get; set; }
    public ProductTransactionType Type { get; set; }
    public int Quantity { get; set; }
    public int StockAfter { get; set; }
}

public class StockHistoryQuery : PagedQuery
{
    public ReceiptSource? Source { get; set; }
    public ProductTransactionType? Type { get; set; }

    // Geçmiş varsayılan olarak en yeniden eskiye sıralanır
    public StockHistoryQuery() => SortDir = "desc";
}

public class ProductTransactionDto
{
    public Guid Id { get; set; }
    public Guid ReceiptId { get; set; }
    public ProductTransactionType Type { get; set; }
    public ReceiptSource Source { get; set; }
    public int Quantity { get; set; }
    public int StockAfter { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}