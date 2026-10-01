namespace ECommerce.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : BaseConfiguration<OrderItem>
{
    protected override void ConfigureEntity(EntityTypeBuilder<OrderItem> b)
    {
        b.ToTable("order_items");
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);

        b.HasOne(x => x.Order).WithMany(o => o.Items)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany(p => p.OrderItems)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}