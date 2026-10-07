using ECommerce.API.Authorization;
using ECommerce.Application.Catalog;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController(IProductService productService, IStockService stockService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> List(
        [FromQuery] ProductListQuery query, CancellationToken ct) =>
        Ok(await productService.ListAsync(query, ct));

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await productService.GetAsync(id, ct));

    [HasPermission(Permissions.Products.Create)]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request, CancellationToken ct)
    {
        var product = await productService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    // Stoğu DEĞİŞTİRMEZ; stok için POST /api/products/{id}/stock kullanılır
    [HasPermission(Permissions.Products.Update)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, UpdateProductRequest request, CancellationToken ct) =>
        Ok(await productService.UpdateAsync(id, request, ct));

    [HasPermission(Permissions.Products.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await productService.DeleteAsync(id, ct);
        return NoContent();
    }

    // Fark (delta) olarak stok girişi/çıkışı. Atomik: eş zamanlı değişiklikler kaybolmaz, stok eksiye düşmez.
    [HasPermission(Permissions.Products.AdjustStock)]
    [HttpPost("{id:guid}/stock")]
    public async Task<ActionResult<StockAdjustmentResultDto>> AdjustStock(
        Guid id, StockAdjustmentRequest request, CancellationToken ct) =>
        Ok(await stockService.AdjustAsync(id, request, ct));

    // Stok hareketleri: sipariş mi, satıcı mı, ilk stok mu
    [HasPermission(Permissions.Products.AdjustStock)]
    [HttpGet("{id:guid}/stock-history")]
    public async Task<ActionResult<PagedResult<ProductTransactionDto>>> StockHistory(
        Guid id, [FromQuery] StockHistoryQuery query, CancellationToken ct) =>
        Ok(await stockService.ListTransactionsAsync(id, query, ct));
}