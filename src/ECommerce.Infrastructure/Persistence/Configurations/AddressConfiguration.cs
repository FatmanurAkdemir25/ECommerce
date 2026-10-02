namespace ECommerce.Infrastructure.Persistence.Configurations;
//Entity lerin EF Core/ veritabanı ayarlarını tutar.

public class AddressConfiguration : BaseConfiguration<Address>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Address> b)
    {
        b.ToTable("addresses");
        b.Property(x => x.Title).HasMaxLength(100);
        b.Property(x => x.City).HasMaxLength(100).IsRequired();
        b.Property(x => x.District).HasMaxLength(100);
        b.Property(x => x.AddressLine).HasMaxLength(500).IsRequired();

        b.HasOne(x => x.User).WithMany(u => u.Addresses)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}