namespace ECommerce.Application.Common.Exceptions;

// Kimliği belli ama bu işlem için yetkisi yok (başkasının verisi, kendi yetkisini değiştirme...)  403
public class ForbiddenException(string message) : Exception(message);