using ECommerce.Application.Abstractions;
using ECommerce.Application.Common.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;
//test amaçlı oluşturuldu.
[ApiController]
[Route("api/diagnostics")]
public class DiagnosticsController(IWebHostEnvironment env, ICacheService cache) : ControllerBase //bana cache servisini ver, ben cache işlemlerini onun üzerinden yapayım
{
    [HttpGet("not-found")]
    public IActionResult NotFoundTest() =>
        !env.IsDevelopment() ? NotFound() : throw new NotFoundException("Product", Guid.NewGuid());

    [HttpGet("conflict")]
    public IActionResult ConflictTest() =>
        !env.IsDevelopment() ? NotFound() : throw new ConflictException("Yetersiz stok.");

    [HttpGet("validation")]
    public IActionResult ValidationTest() =>
        !env.IsDevelopment()
            ? NotFound()
            : throw new ValidationException([
                new ValidationFailure("Email", "E-posta geçersiz."),
                new ValidationFailure("Password", "Şifre en az 8 karakter olmalı.")
            ]);

    [HttpGet("error")]
    public IActionResult ErrorTest() =>
        !env.IsDevelopment() ? NotFound() : throw new InvalidOperationException("Test amaçlı beklenmeyen hata.");

    [HttpGet("cache")]
    public async Task<IActionResult> CacheTest(CancellationToken ct) //cache sisteminin çalışıp çalışmadığı test ediliyor
    {
        if (!env.IsDevelopment()) return NotFound(); //endpoint kullanılmıyor.

        var createdNow = false; //değişken oluşturuluyor. bu değişkenin değeri bize cache den mi geldi yoksa şimdi mi oluşturuldu bilgisini verecek.
        var value = await cache.GetOrCreateAsync("diagnostics:time", _ => //cache e bu isimle değer konuluyor. değer varsa değeri al yoksa oluştur değişkene ata
        {
            createdNow = true;
            return Task.FromResult(DateTime.UtcNow);
        }, TimeSpan.FromSeconds(30), ct);

        return Ok(new { value, createdNow });
    }
}