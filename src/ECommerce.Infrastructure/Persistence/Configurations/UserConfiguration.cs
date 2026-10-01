namespace ECommerce.Infrastructure.Persistence.Configurations;

public class UserConfiguration : BaseConfiguration<User>
{
    protected override void ConfigureEntity(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
    }
}