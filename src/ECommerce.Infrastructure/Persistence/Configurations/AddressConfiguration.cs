namespace ECommerce.Infrastructure.Persistence.Configurations;

public class AddressConfiguration : BaseConfiguration<Address>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Address> b)
    {
        b.ToTable("addresses");
        b.Property(x => x.Title).HasMaxLength(100);
        b.Property(x => x.City).HasMaxLength(100).IsRequired();
        b.Property(x => x.District).HasMaxLength(100);
        b.Property(x => x.AddressLine).HasMaxLength(500).IsRequired();

        b.HasOne(x => x.Customer).WithMany(c => c.Addresses)
            .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}