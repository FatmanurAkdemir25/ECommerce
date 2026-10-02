namespace ECommerce.Application.Common.Exceptions;

// İş kuralı ihlali (yetersiz stok, geçersiz durum geçişi, mükerrer kayıt...) → 409
public class ConflictException(string message) : Exception(message);