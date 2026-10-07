using FluentValidation;

namespace ECommerce.Application.Shopping;

public class AddCartItemRequestValidator : AbstractValidator<AddCartItemRequest>
{
    public AddCartItemRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Ürün zorunludur.");
        RuleFor(x => x.Quantity).InclusiveBetween(1, CartLimits.MaxQuantityPerItem)
            .WithMessage($"Miktar 1 ile {CartLimits.MaxQuantityPerItem} arasında olmalı.");
    }
}

public class SetCartItemQuantityRequestValidator : AbstractValidator<SetCartItemQuantityRequest>
{
    public SetCartItemQuantityRequestValidator()
    {
        // Ürünü sepetten çıkarmak için DELETE kullanılır
        RuleFor(x => x.Quantity).InclusiveBetween(1, CartLimits.MaxQuantityPerItem)
            .WithMessage($"Miktar 1 ile {CartLimits.MaxQuantityPerItem} arasında olmalı.");
    }
}