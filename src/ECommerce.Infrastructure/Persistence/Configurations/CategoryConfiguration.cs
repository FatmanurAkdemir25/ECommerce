namespace ECommerce.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : BaseConfiguration<Category>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Category> b)
    {
        b.ToTable("categories");
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
    }
}