using Serilog.Context;

namespace ECommerce.API.Middleware;
//her HTTP isteğine bir Correlation ID verip o isteğin uygulama boyunca takip edilebilmesini sağlıyo

public class CorrelationIdMiddleware(RequestDelegate next) //next bi sonraki middleware e gönderilmesini sağlıyo
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();//gelen request in header ından Correlation id yi alıyo

        if (string.IsNullOrWhiteSpace(correlationId) //id nin geçerliliği kontrol ediliyor
            || correlationId.Length > 64
            || !correlationId.All(c => char.IsLetterOrDigit(c) || c == '-'))
        {
            correlationId = Guid.NewGuid().ToString("N");//geçersizse yeni id oluşturuluyor
        }

        context.Items[HeaderName] = correlationId; //Burada ID'yi mevcut HTTP request'in Items koleksiyonuna koyuyor
        context.Response.OnStarting(() =>//response gönderilmeden hemen önce header a id ekleniyor. böylece client kendi gönderdiği request in hangi id ile eşlendiğini görebiliyo
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId)) //Bu request devam ettiği sürece oluşturulan loglara CorrelationId bilgisini ekle
        {
            await next(context);
        }
    }
}