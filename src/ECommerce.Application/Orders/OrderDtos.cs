using ECommerce.Application.Common;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Enums;


namespace ECommerce.Application.Orders;

public record CreateOrderRequest(Guid AddressId);
public record UpdateOrderStatusRequest(OrderStatus Status);
public record UpdatePaymentStatusRequest(PaymentStatus Status);

public enum OrderSortBy { CreatedAt, TotalAmount }

// PagedQuery nasıl tanımlıysa (class/record) öyle türet; ProductListQuery'deki gibi.
// SortBy: date | total (varsayılan: date, yeniden eskiye)
public class OrderListQuery : PagedQuery
{
    public OrderStatus? Status { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public Guid? UserId { get; init; }            // sadece personel uçunda kullanılır

    public OrderListQuery() => SortDir = "desc";
}

public record OrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);
public record PaymentDto(PaymentStatus Status, decimal Amount, DateTime? PaidAt);
public record OrderDto(Guid Id, Guid UserId, Guid AddressId, OrderStatus Status, decimal TotalAmount,
    DateTime CreatedAt, List<OrderItemDto> Items, PaymentDto? Payment);
public record OrderSummaryDto(Guid Id, OrderStatus Status, decimal TotalAmount, DateTime CreatedAt,
    int ItemCount, PaymentStatus? PaymentStatus);