using FluentValidation;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Orders;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator() => RuleFor(x => x.AddressId).NotEmpty();
}

public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
        => RuleFor(x => x.Status).IsInEnum()
            .NotEqual(OrderStatus.Cancelled).WithMessage("İptal için POST /api/orders/{id}/cancel kullanın.");
}

public class UpdatePaymentStatusRequestValidator : AbstractValidator<UpdatePaymentStatusRequest>
{
    public UpdatePaymentStatusRequestValidator() => RuleFor(x => x.Status).IsInEnum();
}

public class OrderListQueryValidator : AbstractValidator<OrderListQuery>
{
    public OrderListQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.SortBy)
            .Must(s => string.IsNullOrEmpty(s)
                      || s.Equals("date", StringComparison.OrdinalIgnoreCase)
                      || s.Equals("total", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortBy yalnızca 'date' veya 'total' olabilir.");
        RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithMessage("From, To'dan büyük olamaz.");
    }
}