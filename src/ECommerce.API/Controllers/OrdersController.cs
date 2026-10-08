using ECommerce.API.Authorization;
using ECommerce.Application.Common;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders;
using ECommerce.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderService orders, IPaymentService payments, IAuthorizationService authorization)
    : ControllerBase
{
    // ----- Müşteri -----
    [HttpPost, HasPermission(Permissions.Orders.Create)]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var order = await orders.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetMine), new { id = order.Id }, order);
    }

    [HttpGet("me"), Authorize]
    public Task<PagedResult<OrderSummaryDto>> ListMine([FromQuery] OrderListQuery query, CancellationToken ct)
        => orders.ListMineAsync(query, ct);

    [HttpGet("me/{id:guid}"), Authorize]
    public Task<OrderDto> GetMine(Guid id, CancellationToken ct) => orders.GetMineAsync(id, ct);

    [HttpPost("{id:guid}/cancel"), HasPermission(Permissions.Orders.Cancel)]
    public async Task<OrderDto> Cancel(Guid id, CancellationToken ct)
    {
        // Personel (Orders.ViewAll) herkesin siparişini iptal edebilir; müşteri sadece kendisininkini.
        var isStaff = (await authorization.AuthorizeAsync(User, Permissions.Orders.ViewAll)).Succeeded;
        return await orders.CancelAsync(id, isStaff, ct);
    }

    // ----- Personel -----
    [HttpGet, HasPermission(Permissions.Orders.ViewAll)]
    public Task<PagedResult<OrderSummaryDto>> ListAll([FromQuery] OrderListQuery query, CancellationToken ct)
        => orders.ListAllAsync(query, ct);

    [HttpGet("{id:guid}"), HasPermission(Permissions.Orders.ViewAll)]
    public Task<OrderDto> Get(Guid id, CancellationToken ct) => orders.GetAsync(id, ct);

    [HttpPut("{id:guid}/status"), HasPermission(Permissions.Orders.UpdateStatus)]
    public Task<OrderDto> UpdateStatus(Guid id, UpdateOrderStatusRequest request, CancellationToken ct)
        => orders.UpdateStatusAsync(id, request.Status, ct);

    [HttpPut("{id:guid}/payment/status"), HasPermission(Permissions.Payments.UpdateStatus)]
    public Task<PaymentDto> UpdatePayment(Guid id, UpdatePaymentStatusRequest request, CancellationToken ct)
        => payments.UpdateStatusAsync(id, request.Status, ct);
}