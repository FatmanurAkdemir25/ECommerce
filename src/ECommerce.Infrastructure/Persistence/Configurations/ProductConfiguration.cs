namespace ECommerce.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : BaseConfiguration<Product>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Product> b)
    {
        b.ToTable("products", t =>
            t.HasCheckConstraint("ck_products_stock_non_negative", "stock >= 0"));
        b.Property(x => x.Name).HasMaxLength(250).IsRequired();
        b.Property(x => x.Price).HasPrecision(18, 2);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Category).WithMany(c => c.Products)
            .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}