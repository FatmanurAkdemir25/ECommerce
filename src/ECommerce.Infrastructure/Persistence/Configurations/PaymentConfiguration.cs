namespace ECommerce.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : BaseConfiguration<Payment>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("payments");
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Amount).HasPrecision(18, 2);

        b.HasOne(x => x.Order).WithOne(o => o.Payment)
            .HasForeignKey<Payment>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}