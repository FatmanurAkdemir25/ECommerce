using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

// Stok fişi: stokun NEDEN değiştiğini tutar. Sadece eklenir, güncellenmez ve silinmez.
public class Receipt : BaseEntity
{
    public ReceiptSource Source { get; set; }

    // Source = Order / OrderCancellation ise dolu
    public Guid? OrderId { get; set; }

    // Source = SellerAdjustment / InitialStock ise dolu
    public Guid? PerformedByUserId { get; set; }

    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Order? Order { get; set; }
    public User? PerformedBy { get; set; }
    public ICollection<ProductTransaction> Transactions { get; set; } = new List<ProductTransaction>();
}