using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ECommerce.API.Filters;

public class ValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter //Action Filter kullanarak gelen request modellerini otomatik olarak FluentValidation ile doğruluyo. yani controller dan gelen dto yu tek tek kod yazmadan kontrol etmemizi sağlıyo
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (serviceProvider.GetService(validatorType) is not IValidator validator) continue; //şu tipe ait servis var mı? ilgili dto nun validator unu bulmak için

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument), context.HttpContext.RequestAborted); //validation yapılıyor

            if (!result.IsValid)
            {
                // ExceptionHandlingMiddleware bunu 400'e çevirir
                throw new ValidationException(result.Errors);
            }
        }

        await next();//Validation tamam, controller action'ının çalışmasına izin ver
    }
}