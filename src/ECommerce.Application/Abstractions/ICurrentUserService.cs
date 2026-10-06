namespace ECommerce.Application.Abstractions;

public interface ICurrentUserService
{
    // Token'daki "sub" claim'i; kimliksiz istekte null
    Guid? UserId { get; }
}