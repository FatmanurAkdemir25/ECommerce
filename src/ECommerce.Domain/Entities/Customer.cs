using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Customer : BaseEntity
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }

    public User User { get; set; } = null!;
    public Cart? Cart { get; set; }
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}