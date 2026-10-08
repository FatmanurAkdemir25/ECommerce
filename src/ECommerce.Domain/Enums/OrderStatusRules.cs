namespace ECommerce.Domain.Enums;

public static class OrderStatusRules
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = [],
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to)
        => Allowed.TryGetValue(from, out var next) && next.Contains(to);
}

public static class PaymentStatusRules
{
    public static bool CanTransition(PaymentStatus from, PaymentStatus to)
        => from == PaymentStatus.Pending && to is PaymentStatus.Paid or PaymentStatus.Cancelled;
}