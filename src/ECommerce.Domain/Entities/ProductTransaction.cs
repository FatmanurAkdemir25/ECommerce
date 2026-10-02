using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

// Fiş satırı: hangi üründe ne kadar giriş/çıkış olduğunu tutar.
public class ProductTransaction : BaseEntity
{
    public Guid ReceiptId { get; set; }
    public Guid ProductId { get; set; }
    public ProductTransactionType Type { get; set; }

    // Her zaman pozitif, yön Type alanından gelir
    public int Quantity { get; set; }

    // Bu satırdan sonraki stok
    public int StockAfter { get; set; }

    // Ürün geçmişini fişe join atmadan tarihe göre listelemek için fişten kopyalanır
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Receipt Receipt { get; set; } = null!;
    public Product Product { get; set; } = null!;
}