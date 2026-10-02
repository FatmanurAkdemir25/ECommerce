using ECommerce.Domain.Common;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class ReceiptConfiguration : BaseConfiguration<Receipt>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Receipt> b)
    {
        // Fişin kaynağı ile referansı tutarlı olmalı: sipariş fişinde sipariş, satıcı fişinde kullanıcı dolu
        b.ToTable("receipts", t => t.HasCheckConstraint(
            "ck_receipts_source_reference",
            "(source IN ('Order', 'OrderCancellation') AND order_id IS NOT NULL) " +
            "OR (source IN ('SellerAdjustment', 'InitialStock') AND performed_by_user_id IS NOT NULL)"));

        b.Property(x => x.Source).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasIndex(x => x.OrderId);

        // Defter kayıtları hiçbir ilişki yüzünden otomatik silinmesin: hepsi Restrict
        b.HasOne(x => x.Order).WithMany(o => o.Receipts)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PerformedBy).WithMany(u => u.Receipts)
            .HasForeignKey(x => x.PerformedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}