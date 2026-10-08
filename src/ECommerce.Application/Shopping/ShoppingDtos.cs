namespace ECommerce.Application.Shopping;

public static class CartLimits
{
    public const int MaxDistinctItems = 50;
    public const int MaxQuantityPerItem = 100;
}

public class CartItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    // Güncel fiyat. Fiyat sipariş anında sabitlenir (Adım 10)
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int AvailableStock { get; set; }
    public bool IsProductActive { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;

    // Kalem sorunluysa nedeni (ürün pasif, stok yetersiz). Sorun yoksa null
    public string? Issue { get; set; }
    public bool IsAvailable => Issue is null;
}

public class CartDto
{
    public DateTime? UpdatedAt { get; set; }
    public List<CartItemDto> Items { get; set; } = [];

    // Sepetteki toplam adet
    public int TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public bool HasIssues { get; set; }
}

public class AddCartItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class SetCartItemQuantityRequest
{
    public int Quantity { get; set; }
}