using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Product : BaseEntity
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Concurrency token (stok çakışmaları için), Adım 3'te yapılandırılacak
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Category Category { get; set; } = null!;
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}