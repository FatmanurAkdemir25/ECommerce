using ECommerce.Application.Common;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Orders;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken ct);
    Task<PagedResult<OrderSummaryDto>> ListMineAsync(OrderListQuery query, CancellationToken ct);
    Task<OrderDto> GetMineAsync(Guid id, CancellationToken ct);
    Task<PagedResult<OrderSummaryDto>> ListAllAsync(OrderListQuery query, CancellationToken ct);
    Task<OrderDto> GetAsync(Guid id, CancellationToken ct);
    Task<OrderDto> UpdateStatusAsync(Guid id, OrderStatus status, CancellationToken ct);
    Task<OrderDto> CancelAsync(Guid id, bool isStaff, CancellationToken ct);
}

public interface IPaymentService
{
    Task<PaymentDto> UpdateStatusAsync(Guid orderId, PaymentStatus status, CancellationToken ct);
}