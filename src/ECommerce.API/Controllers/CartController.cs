using ECommerce.API.Authorization;
using ECommerce.Application.Shopping;
using ECommerce.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

// Giriş yapmış kullanıcının KENDİ sepeti. Kullanıcı Id'si token'dan gelir.
[ApiController]
[Route("api/cart")]
[Produces("application/json")]
[HasPermission(Permissions.Carts.Manage)]
public class CartController(ICartService cartService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CartDto>> Get(CancellationToken ct) =>
        Ok(await cartService.GetAsync(ct));

    // Aynı ürün sepetteyse miktar artar
    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(AddCartItemRequest request, CancellationToken ct) =>
        Ok(await cartService.AddItemAsync(request, ct));

    // Miktarı belirler (artırmaz). Ürünü çıkarmak için DELETE kullanılır
    [HttpPut("items/{productId:guid}")]
    public async Task<ActionResult<CartDto>> SetQuantity(
        Guid productId, SetCartItemQuantityRequest request, CancellationToken ct) =>
        Ok(await cartService.SetQuantityAsync(productId, request, ct));

    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid productId, CancellationToken ct)
    {
        await cartService.RemoveItemAsync(productId, ct);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        await cartService.ClearAsync(ct);
        return NoContent();
    }
}