namespace ECommerce.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : BaseConfiguration<Order>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Order> b)
    {
        b.ToTable("orders");
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);

        b.HasOne(x => x.User).WithMany(u => u.Orders)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Address).WithMany(a => a.Orders)
            .HasForeignKey(x => x.AddressId).OnDelete(DeleteBehavior.Restrict);
    }
}