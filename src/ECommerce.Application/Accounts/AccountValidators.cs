using FluentValidation;

namespace ECommerce.Application.Accounts;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Ad soyad zorunludur.")
            .MaximumLength(200).WithMessage("Ad soyad en fazla 200 karakter olabilir.");

        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("Telefon en fazla 30 karakter olabilir.")
            .Matches(@"^[0-9+\-\s()]+$").WithMessage("Telefon sadece rakam ve + - ( ) karakterlerini içerebilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}

public class CreateAddressRequestValidator : AbstractValidator<CreateAddressRequest>
{
    public CreateAddressRequestValidator()
    {
        RuleFor(x => x.Title).MaximumLength(100).WithMessage("Başlık en fazla 100 karakter olabilir.");
        RuleFor(x => x.City).NotEmpty().WithMessage("Şehir zorunludur.")
            .MaximumLength(100).WithMessage("Şehir en fazla 100 karakter olabilir.");
        RuleFor(x => x.District).MaximumLength(100).WithMessage("İlçe en fazla 100 karakter olabilir.");
        RuleFor(x => x.AddressLine).NotEmpty().WithMessage("Adres satırı zorunludur.")
            .MaximumLength(500).WithMessage("Adres satırı en fazla 500 karakter olabilir.");
    }
}

public class UpdateAddressRequestValidator : AbstractValidator<UpdateAddressRequest>
{
    public UpdateAddressRequestValidator()
    {
        RuleFor(x => x.Title).MaximumLength(100).WithMessage("Başlık en fazla 100 karakter olabilir.");
        RuleFor(x => x.City).NotEmpty().WithMessage("Şehir zorunludur.")
            .MaximumLength(100).WithMessage("Şehir en fazla 100 karakter olabilir.");
        RuleFor(x => x.District).MaximumLength(100).WithMessage("İlçe en fazla 100 karakter olabilir.");
        RuleFor(x => x.AddressLine).NotEmpty().WithMessage("Adres satırı zorunludur.")
            .MaximumLength(500).WithMessage("Adres satırı en fazla 500 karakter olabilir.");
    }
}

public class UserListQueryValidator : AbstractValidator<UserListQuery>
{
    private static readonly string[] AllowedSorts = ["name", "email", "date", "createdat"];

    public UserListQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.SortBy).Must(s => s is null || AllowedSorts.Contains(s.ToLowerInvariant()))
            .WithMessage("SortBy şunlardan biri olmalı: name, email, date.");
        RuleFor(x => x.SortDir).Must(d => d.ToLowerInvariant() is "asc" or "desc")
            .WithMessage("SortDir 'asc' veya 'desc' olmalı.");
    }
}