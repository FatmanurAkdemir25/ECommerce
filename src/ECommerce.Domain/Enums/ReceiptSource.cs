namespace ECommerce.Domain.Enums;

public enum ReceiptSource
{
    InitialStock,       // Ürün oluşturulurken verilen ilk stok
    SellerAdjustment,   // Satıcı/personel stok girişi veya düzeltmesi
    Order,              // Sipariş nedeniyle düşüm
    OrderCancellation   // Sipariş iptali nedeniyle iade
}