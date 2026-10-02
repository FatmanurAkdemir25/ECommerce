namespace ECommerce.Infrastructure.Persistence.Configurations;

public class ProductTransactionConfiguration : BaseConfiguration<ProductTransaction>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProductTransaction> b)
    {
        b.ToTable("product_transactions", t =>
            t.HasCheckConstraint("ck_product_transactions_quantity_positive", "quantity > 0"));

        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(10);

        // Bir fişte aynı ürün tek satır olur
        b.HasIndex(x => new { x.ReceiptId, x.ProductId }).IsUnique();

        // Bir ürünün geçmişini tarihe göre listelemek için
        b.HasIndex(x => new { x.ProductId, x.CreatedAt });

        b.HasOne(x => x.Receipt).WithMany(r => r.Transactions)
            .HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Product).WithMany(p => p.Transactions)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}