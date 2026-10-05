namespace ECommerce.Application.Common.Exceptions;

// Kimlik doğrulama başarısız (yanlış şifre, geçersiz/iptal edilmiş token) → 401
public class UnauthorizedException(string message) : Exception(message);