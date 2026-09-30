using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Address : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string? Title { get; set; }
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public Customer Customer { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}