namespace ECommerce.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : BaseConfiguration<Role>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Role> b)
    {
        b.ToTable("roles");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
        b.Property(x => x.Description).HasMaxLength(250);
    }
}