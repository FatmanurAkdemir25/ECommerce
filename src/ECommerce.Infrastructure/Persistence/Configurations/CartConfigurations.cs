namespace ECommerce.Infrastructure.Persistence.Configurations;

public class CartConfiguration : BaseConfiguration<Cart>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Cart> b)
    {
        b.ToTable("carts");
        b.HasOne(x => x.User).WithOne(u => u.Cart)
            .HasForeignKey<Cart>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}