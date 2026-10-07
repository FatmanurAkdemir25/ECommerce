using AutoMapper;
using AutoMapper.QueryableExtensions;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Accounts;

public class AddressService(
    IAppDbContext db,
    IMapper mapper,
    ICurrentUserService currentUser,
    ILogger<AddressService> logger) : IAddressService
{
    private const int MaxAddressesPerUser = 20;

    public async Task<IReadOnlyList<AddressDto>> ListMineAsync(CancellationToken ct)
    {
        var userId = await UserGuard.RequireActiveUserAsync(currentUser, db, ct);
        return await LoadListAsync(userId, ct);
    }

    public async Task<IReadOnlyList<AddressDto>> ListForUserAsync(Guid userId, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
        {
            throw new NotFoundException("Kullanıcı", userId);
        }

        return await LoadListAsync(userId, ct);
    }

    public async Task<AddressDto> GetMineAsync(Guid id, CancellationToken ct)
    {
        var userId = await UserGuard.RequireActiveUserAsync(currentUser, db, ct);

        // Başkasının adresi için de 404: varlığını bile belli etmiyoruz
        var address = await db.Addresses.AsNoTracking()
            .Where(a => a.Id == id && a.UserId == userId)
            .ProjectTo<AddressDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);

        return address ?? throw new NotFoundException("Adres", id);
    }

    public async Task<AddressDto> CreateAsync(CreateAddressRequest request, CancellationToken ct)
    {
        var userId = await UserGuard.RequireActiveUserAsync(currentUser, db, ct);

        var count = await db.Addresses.CountAsync(a => a.UserId == userId, ct);
        if (count >= MaxAddressesPerUser)
        {
            throw new ConflictException($"En fazla {MaxAddressesPerUser} adres kaydedebilirsiniz.");
        }

        // İlk adres her zaman varsayılan olur
        var makeDefault = request.IsDefault || count == 0;
        if (makeDefault)
        {
            await ClearDefaultsAsync(userId, ct);
        }

        var address = new Address
        {
            UserId = userId,
            Title = CleanText(request.Title),
            City = request.City.Trim(),
            District = CleanText(request.District),
            AddressLine = request.AddressLine.Trim(),
            IsDefault = makeDefault
        };
        db.Addresses.Add(address);

        // Eski varsayılanın kaldırılması ve yeni adres tek SaveChanges içinde (tek transaction)
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Adres eklendi. UserId: {UserId}, AddressId: {AddressId}", userId, address.Id);
        return mapper.Map<AddressDto>(address);
    }

    public async Task<AddressDto> UpdateAsync(Guid id, UpdateAddressRequest request, CancellationToken ct)
    {
        var userId = await UserGuard.RequireActiveUserAsync(currentUser, db, ct);
        var address = await FindOwnedAsync(id, userId, ct);
        await EnsureNotUsedByOrderAsync(id, "değiştirilemez", ct);

        address.Title = CleanText(request.Title);
        address.City = request.City.Trim();
        address.District = CleanText(request.District);
        address.AddressLine = request.AddressLine.Trim();
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Adres güncellendi. UserId: {UserId}, AddressId: {AddressId}", userId, id);
        return mapper.Map<AddressDto>(address);
    }

    public async Task SetDefaultAsync(Guid id, CancellationToken ct)
    {
        var userId = await UserGuard.RequireActiveUserAsync(currentUser, db, ct);

        // Kullanıcının tüm adresleri (en fazla 20) yüklenir: hedef dışındakilerin hepsi false olur.
        // Bu, olası bir tutarsızlığı (birden fazla varsayılan) da kendiliğinden düzeltir.
        var addresses = await db.Addresses.Where(a => a.UserId == userId).ToListAsync(ct);
        if (addresses.All(a => a.Id != id))
        {
            throw new NotFoundException("Adres", id);
        }

        foreach (var address in addresses)
        {
            address.IsDefault = address.Id == id;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Varsayılan adres değişti. UserId: {UserId}, AddressId: {AddressId}", userId, id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var userId = await UserGuard.RequireActiveUserAsync(currentUser, db, ct);
        var address = await FindOwnedAsync(id, userId, ct);
        await EnsureNotUsedByOrderAsync(id, "silinemez", ct);

        // Varsayılan silinirse kalan adreslerden biri devralır (her zaman bir varsayılan olmalı)
        if (address.IsDefault)
        {
            var next = await db.Addresses
                .Where(a => a.UserId == userId && a.Id != id)
                .OrderBy(a => a.Id)
                .FirstOrDefaultAsync(ct);

            if (next is not null) next.IsDefault = true;
        }

        db.Addresses.Remove(address);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Adres silindi. UserId: {UserId}, AddressId: {AddressId}", userId, id);
    }

    private async Task<IReadOnlyList<AddressDto>> LoadListAsync(Guid userId, CancellationToken ct) =>
        await db.Addresses.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault).ThenBy(a => a.Title).ThenBy(a => a.Id)
            .ProjectTo<AddressDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

    private async Task<Address> FindOwnedAsync(Guid id, Guid userId, CancellationToken ct) =>
        await db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct)
            ?? throw new NotFoundException("Adres", id);

    private async Task ClearDefaultsAsync(Guid userId, CancellationToken ct)
    {
        var defaults = await db.Addresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync(ct);
        foreach (var existing in defaults) existing.IsDefault = false;
    }

    // Sipariş sadece address_id tutuyor. Kullanılmış adres değişirse geçmiş siparişlerin
    // teslimat adresi de sessizce değişir, bu yüzden kilitlenir.
    private async Task EnsureNotUsedByOrderAsync(Guid addressId, string action, CancellationToken ct)
    {
        if (await db.Orders.AnyAsync(o => o.AddressId == addressId, ct))
        {
            throw new ConflictException(
                $"Bu adres bir siparişte kullanıldığı için {action}. Yeni bir adres ekleyebilirsiniz.");
        }
    }

    private static string? CleanText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}