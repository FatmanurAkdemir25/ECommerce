namespace ECommerce.Infrastructure.Persistence.Configurations;

public class CartConfiguration : BaseConfiguration<Cart>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Cart> b)
    {
        b.ToTable("carts");
        b.HasOne(x => x.Customer).WithOne(c => c.Cart)
            .HasForeignKey<Cart>(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}