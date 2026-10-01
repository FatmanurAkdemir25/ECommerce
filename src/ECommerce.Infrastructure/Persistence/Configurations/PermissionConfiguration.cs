namespace ECommerce.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : BaseConfiguration<Permission>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("permissions");
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
        b.Property(x => x.Description).HasMaxLength(250);
    }
}