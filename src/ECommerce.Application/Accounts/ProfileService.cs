using AutoMapper;
using AutoMapper.QueryableExtensions;
using ECommerce.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Accounts;

public class ProfileService(
    IAppDbContext db,
    IMapper mapper,
    ICurrentUserService currentUser,
    ILogger<ProfileService> logger) : IProfileService
{
    public async Task<ProfileDto> GetMyProfileAsync(CancellationToken ct)
    {
        var userId = await UserGuard.RequireActiveUserAsync(currentUser, db, ct);

        return await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .ProjectTo<ProfileDto>(mapper.ConfigurationProvider)
            .FirstAsync(ct);
    }

    public async Task<ProfileDto> UpdateMyProfileAsync(UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = await UserGuard.RequireActiveUserAsync(currentUser, db, ct);
        var user = await db.Users.FirstAsync(u => u.Id == userId, ct);

        user.FullName = request.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Profil güncellendi. UserId: {UserId}", userId);
        return mapper.Map<ProfileDto>(user);
    }
}