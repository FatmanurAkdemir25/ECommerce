using FluentValidation;

namespace ECommerce.Application.Authorization;

public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Rol adı zorunludur.")
            .MaximumLength(100).WithMessage("Rol adı en fazla 100 karakter olabilir.");
        RuleFor(x => x.Description).MaximumLength(250).WithMessage("Açıklama en fazla 250 karakter olabilir.");
    }
}

public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Rol adı zorunludur.")
            .MaximumLength(100).WithMessage("Rol adı en fazla 100 karakter olabilir.");
        RuleFor(x => x.Description).MaximumLength(250).WithMessage("Açıklama en fazla 250 karakter olabilir.");
    }
}

public class CreatePermissionRequestValidator : AbstractValidator<CreatePermissionRequest>
{
    public CreatePermissionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("İzin adı zorunludur.")
            .MaximumLength(150).WithMessage("İzin adı en fazla 150 karakter olabilir.")
            .Matches(@"^[A-Z][A-Za-z0-9]*\.[A-Z][A-Za-z0-9]*$")
            .WithMessage("İzin adı 'Kaynak.Eylem' formatında olmalı (örn. Products.Create).");
        RuleFor(x => x.Description).MaximumLength(250).WithMessage("Açıklama en fazla 250 karakter olabilir.");
    }
}

public class RoleListQueryValidator : AbstractValidator<RoleListQuery>
{
    public RoleListQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.SortDir).Must(d => d is "asc" or "desc")
            .WithMessage("SortDir 'asc' veya 'desc' olmalı.");
    }
}

public class PermissionListQueryValidator : AbstractValidator<PermissionListQuery>
{
    public PermissionListQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.SortDir).Must(d => d is "asc" or "desc")
            .WithMessage("SortDir 'asc' veya 'desc' olmalı.");
    }
}