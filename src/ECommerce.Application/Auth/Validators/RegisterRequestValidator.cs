using FluentValidation;

namespace ECommerce.Application.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .MaximumLength(256).WithMessage("E-posta en fazla 256 karakter olabilir.")
            .EmailAddress().WithMessage("E-posta formatı geçersiz.");

        // Üst sınır, çok uzun şifrelerle hash hesaplamasını yavaşlatma saldırısını engeller
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalı.")
            .MaximumLength(100).WithMessage("Şifre en fazla 100 karakter olabilir.")
            .Matches("[A-Z]").WithMessage("Şifre en az bir büyük harf içermeli.")
            .Matches("[a-z]").WithMessage("Şifre en az bir küçük harf içermeli.")
            .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermeli.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad soyad zorunludur.")
            .MaximumLength(200).WithMessage("Ad soyad en fazla 200 karakter olabilir.");

        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("Telefon en fazla 30 karakter olabilir.")
            .Matches(@"^[0-9+\-\s()]+$").WithMessage("Telefon sadece rakam ve + - ( ) karakterlerini içerebilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}