using AutoMapper;
using AutoMapper.QueryableExtensions;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Orders;

public class PaymentService(IAppDbContext db, IMapper mapper, TimeProvider clock) : IPaymentService
{
    public async Task<PaymentDto> UpdateStatusAsync(Guid orderId, PaymentStatus status, CancellationToken ct)
    {
        var payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.OrderId == orderId, ct)
            ?? throw new NotFoundException("Ödeme kaydı bulunamadı.");

        if (!PaymentStatusRules.CanTransition(payment.Status, status))
            throw new ConflictException($"Ödeme {payment.Status} durumundan {status} durumuna geçirilemez.");

        var from = payment.Status;
        DateTime? paidAt = status == PaymentStatus.Paid ? clock.GetUtcNow().UtcDateTime : null;

        var updated = await db.Payments.Where(p => p.Id == payment.Id && p.Status == from)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, status).SetProperty(p => p.PaidAt, paidAt), ct);
        if (updated == 0)
            throw new ConflictException("Ödeme durumu başka bir işlemle değişti. Tekrar deneyin.");

        return await db.Payments.AsNoTracking().Where(p => p.Id == payment.Id)
            .ProjectTo<PaymentDto>(mapper.ConfigurationProvider).FirstAsync(ct);
    }
}