namespace ECommerce.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : BaseConfiguration<RefreshToken>
{
    protected override void ConfigureEntity(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.Property(x => x.Token).HasMaxLength(512).IsRequired();
        b.HasIndex(x => x.Token).IsUnique();
        b.Property(x => x.ReplacedByToken).HasMaxLength(512);

        // Hesaplanan property'ler, kolon olmayacak
        b.Ignore(x => x.IsRevoked);
        b.Ignore(x => x.IsExpired);
        b.Ignore(x => x.IsActive);

        b.HasOne(x => x.User).WithMany(u => u.RefreshTokens)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}