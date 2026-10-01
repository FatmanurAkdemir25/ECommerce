namespace ECommerce.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : BaseConfiguration<Customer>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("customers");
        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(30);

        // 1-1: otomatik unique index oluşur
        b.HasOne(x => x.User).WithOne(u => u.Customer)
            .HasForeignKey<Customer>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}