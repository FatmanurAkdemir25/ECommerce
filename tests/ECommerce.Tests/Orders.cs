using ECommerce.Domain.Enums;
using Xunit;

namespace ECommerce.Tests.Orders;

public class StatusRulesTests
{
    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered)]
    public void Allowed_transitions(OrderStatus from, OrderStatus to)
        => Assert.True(OrderStatusRules.CanTransition(from, to));

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Pending, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Pending)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed)]
    public void Forbidden_transitions(OrderStatus from, OrderStatus to)
        => Assert.False(OrderStatusRules.CanTransition(from, to));

    [Theory]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Paid, true)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Cancelled, true)]
    [InlineData(PaymentStatus.Paid, PaymentStatus.Pending, false)]
    [InlineData(PaymentStatus.Paid, PaymentStatus.Cancelled, false)]
    [InlineData(PaymentStatus.Cancelled, PaymentStatus.Paid, false)]
    public void Payment_transitions(PaymentStatus from, PaymentStatus to, bool expected)
        => Assert.Equal(expected, PaymentStatusRules.CanTransition(from, to));
}