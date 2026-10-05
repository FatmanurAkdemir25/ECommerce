using FluentValidation;

namespace ECommerce.Application.Auth.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token zorunludur.")
            .MaximumLength(200).WithMessage("Refresh token geçersiz.");
    }
}