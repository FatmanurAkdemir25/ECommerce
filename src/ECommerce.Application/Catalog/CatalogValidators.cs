using FluentValidation;

namespace ECommerce.Application.Catalog;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Kategori adı zorunludur.")
            .MaximumLength(150).WithMessage("Kategori adı en fazla 150 karakter olabilir.");
    }
}

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Kategori adı zorunludur.")
            .MaximumLength(150).WithMessage("Kategori adı en fazla 150 karakter olabilir.");
    }
}

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Kategori zorunludur.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Ürün adı zorunludur.")
            .MaximumLength(250).WithMessage("Ürün adı en fazla 250 karakter olabilir.");
        RuleFor(x => x.Description).MaximumLength(4000).WithMessage("Açıklama en fazla 4000 karakter olabilir.");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Fiyat sıfırdan büyük olmalı.")
            .LessThanOrEqualTo(10_000_000m).WithMessage("Fiyat en fazla 10.000.000 olabilir.")
            .PrecisionScale(18, 2, true).WithMessage("Fiyat en fazla 2 ondalık basamak içerebilir.");
        RuleFor(x => x.Stock).InclusiveBetween(0, 1_000_000).WithMessage("Stok 0 ile 1.000.000 arasında olmalı.");
    }
}

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Kategori zorunludur.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Ürün adı zorunludur.")
            .MaximumLength(250).WithMessage("Ürün adı en fazla 250 karakter olabilir.");
        RuleFor(x => x.Description).MaximumLength(4000).WithMessage("Açıklama en fazla 4000 karakter olabilir.");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Fiyat sıfırdan büyük olmalı.")
            .LessThanOrEqualTo(10_000_000m).WithMessage("Fiyat en fazla 10.000.000 olabilir.")
            .PrecisionScale(18, 2, true).WithMessage("Fiyat en fazla 2 ondalık basamak içerebilir.");
    }
}

public class StockAdjustmentRequestValidator : AbstractValidator<StockAdjustmentRequest>
{
    public StockAdjustmentRequestValidator()
    {
        RuleFor(x => x.Type).NotNull().WithMessage("İşlem türü zorunludur (In veya Out).")
            .IsInEnum().WithMessage("İşlem türü geçersiz (In veya Out).");
        RuleFor(x => x.Quantity).InclusiveBetween(1, 1_000_000).WithMessage("Miktar 1 ile 1.000.000 arasında olmalı.");
        RuleFor(x => x.Note).MaximumLength(500).WithMessage("Not en fazla 500 karakter olabilir.");
    }
}

public class CategoryListQueryValidator : AbstractValidator<CategoryListQuery>
{
    public CategoryListQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.SortDir).Must(d => d.ToLowerInvariant() is "asc" or "desc")
            .WithMessage("SortDir 'asc' veya 'desc' olmalı.");
    }
}

public class ProductListQueryValidator : AbstractValidator<ProductListQuery>
{
    private static readonly string[] AllowedSorts = ["price", "name", "date", "createdat"];

    public ProductListQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue)
            .WithMessage("Minimum fiyat negatif olamaz.");
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue)
            .WithMessage("Maksimum fiyat negatif olamaz.");
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(x => x.MinPrice)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
            .WithMessage("Maksimum fiyat minimum fiyattan küçük olamaz.");
        RuleFor(x => x.SortBy).Must(s => s is null || AllowedSorts.Contains(s.ToLowerInvariant()))
            .WithMessage("SortBy şunlardan biri olmalı: price, name, date.");
        RuleFor(x => x.SortDir).Must(d => d.ToLowerInvariant() is "asc" or "desc")
            .WithMessage("SortDir 'asc' veya 'desc' olmalı.");
    }
}

public class StockHistoryQueryValidator : AbstractValidator<StockHistoryQuery>
{
    public StockHistoryQueryValidator()
    {
        RuleFor(x => x.SortDir).Must(d => d.ToLowerInvariant() is "asc" or "desc")
            .WithMessage("SortDir 'asc' veya 'desc' olmalı.");
    }
}