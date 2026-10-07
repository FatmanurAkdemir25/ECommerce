using AutoMapper;
using AutoMapper.QueryableExtensions;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Catalog;

public class StockService(
    IAppDbContext db,
    IMapper mapper,
    ICacheService cache,
    ICurrentUserService currentUser,
    ILogger<StockService> logger) : IStockService
{
    public async Task<Receipt> ApplyAsync(
        ReceiptSource source,
        IReadOnlyList<StockChange> changes,
        Guid? orderId,
        Guid? performedByUserId,
        string? note,
        CancellationToken ct)
    {
        // Aynı ürün birden fazla gelirse birleştirilir. Kilitler hep aynı sırayla alınır (deadlock riskini azaltır).
        var merged = changes
            .GroupBy(c => c.ProductId)
            .Select(g => new StockChange(g.Key, g.Sum(c => c.Delta)))
            .Where(c => c.Delta != 0)
            .OrderBy(c => c.ProductId)
            .ToList();

        if (merged.Count == 0)
        {
            throw new ArgumentException("En az bir stok değişikliği gerekli.", nameof(changes));
        }

        // Dış transaction yoksa kendimiz açarız; hata olursa dispose'da otomatik geri alınır
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction ? await db.Database.BeginTransactionAsync(ct) : null;

        var receipt = new Receipt
        {
            Source = source,
            OrderId = orderId,
            PerformedByUserId = performedByUserId,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };

        foreach (var change in merged)
        {
            var productId = change.ProductId;
            var delta = change.Delta;

            // Tek atomik UPDATE: oku-değiştir-yaz yok. Eş zamanlı değişiklikler kaybolmaz, stok eksiye düşmez.
            var affected = await db.Products
                .Where(p => p.Id == productId && p.Stock + delta >= 0)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + delta), ct);

            // Satır güncellendiyse yeni stok, güncellenmediyse mevcut stok döner. Satır kilitli olduğu için tutarlıdır.
            var stockAfter = await db.Products.AsNoTracking()
                .Where(p => p.Id == productId)
                .Select(p => (int?)p.Stock)
                .FirstOrDefaultAsync(ct);

            if (stockAfter is null) throw new NotFoundException("Ürün", productId);

            if (affected == 0)
            {
                throw new ConflictException(
                    $"Yetersiz stok. Ürün: {productId}, mevcut: {stockAfter}, istenen çıkış: {-delta}.");
            }

            receipt.Transactions.Add(new ProductTransaction
            {
                ProductId = productId,
                Type = delta > 0 ? ProductTransactionType.In : ProductTransactionType.Out,
                Quantity = Math.Abs(delta),
                StockAfter = stockAfter.Value
            });
        }

        db.Receipts.Add(receipt);
        await db.SaveChangesAsync(ct);

        if (transaction is not null)
        {
            await transaction.CommitAsync(ct);

            // Cache, commit'ten SONRA temizlenir; aksi halde eski stok yeniden cache'lenebilir
            foreach (var change in merged)
            {
                await cache.RemoveAsync(CacheKeys.Product(change.ProductId), ct);
            }
        }

        return receipt;
    }

    public async Task<StockAdjustmentResultDto> AdjustAsync(
        Guid productId, StockAdjustmentRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("Kimlik doğrulanamadı.");
        var delta = request.Type == ProductTransactionType.In ? request.Quantity : -request.Quantity;

        var receipt = await ApplyAsync(
            ReceiptSource.SellerAdjustment, [new StockChange(productId, delta)], null, userId, request.Note, ct);

        var line = receipt.Transactions.Single();
        logger.LogInformation(
            "Stok güncellendi. ProductId: {ProductId}, Type: {Type}, Quantity: {Quantity}, StockAfter: {StockAfter}, ActorId: {ActorId}",
            productId, line.Type, line.Quantity, line.StockAfter, userId);

        return new StockAdjustmentResultDto
        {
            ProductId = productId,
            ReceiptId = receipt.Id,
            Type = line.Type,
            Quantity = line.Quantity,
            StockAfter = line.StockAfter
        };
    }

    public async Task<PagedResult<ProductTransactionDto>> ListTransactionsAsync(
        Guid productId, StockHistoryQuery query, CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId, ct))
        {
            throw new NotFoundException("Ürün", productId);
        }

        var lines = db.ProductTransactions.AsNoTracking().Where(t => t.ProductId == productId);

        if (query.Source is { } source) lines = lines.Where(t => t.Receipt.Source == source);
        if (query.Type is { } type) lines = lines.Where(t => t.Type == type);

        var total = await lines.CountAsync(ct);

        var ordered = query.IsDescending
            ? lines.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
            : lines.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id);

        var items = await ordered.Skip(query.Skip).Take(query.PageSize)
            .ProjectTo<ProductTransactionDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return PagedResult<ProductTransactionDto>.Create(items, total, query);
    }
}