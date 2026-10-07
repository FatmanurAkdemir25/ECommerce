namespace ECommerce.Application.Shopping;

public interface ICartService
{
    Task<CartDto> GetAsync(CancellationToken ct);
    Task<CartDto> AddItemAsync(AddCartItemRequest request, CancellationToken ct);
    Task<CartDto> SetQuantityAsync(Guid productId, SetCartItemQuantityRequest request, CancellationToken ct);
    Task RemoveItemAsync(Guid productId, CancellationToken ct);
    Task ClearAsync(CancellationToken ct);
}