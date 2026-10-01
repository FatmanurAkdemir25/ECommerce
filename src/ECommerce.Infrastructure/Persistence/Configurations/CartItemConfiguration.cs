namespace ECommerce.Infrastructure.Persistence.Configurations;

public class CartItemConfiguration : BaseConfiguration<CartItem>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CartItem> b)
    {
        b.ToTable("cart_items");
        b.HasIndex(x => new { x.CartId, x.ProductId }).IsUnique();

        b.HasOne(x => x.Cart).WithMany(c => c.Items)
            .HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany(p => p.CartItems)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}