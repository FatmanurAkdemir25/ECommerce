using AutoMapper;
using AutoMapper.QueryableExtensions;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Accounts;

public class UserDirectoryService(IAppDbContext db, IMapper mapper) : IUserDirectoryService
{
    public async Task<PagedResult<UserSummaryDto>> ListAsync(UserListQuery query, CancellationToken ct)
    {
        var users = db.Users.AsNoTracking();

        if (query.IsActive is { } active) users = users.Where(u => u.IsActive == active);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            users = users.Where(u => u.Email.Contains(search) || u.FullName.Contains(search));
        }

        var total = await users.CountAsync(ct);

        // Sayfalar arasında kayıt atlanmasın / tekrarlanmasın diye ikincil sıralama Id
        var ordered = (query.SortBy?.ToLowerInvariant(), query.IsDescending) switch
        {
            ("email", false) => users.OrderBy(u => u.Email).ThenBy(u => u.Id),
            ("email", true) => users.OrderByDescending(u => u.Email).ThenBy(u => u.Id),
            ("date" or "createdat", false) => users.OrderBy(u => u.CreatedAt).ThenBy(u => u.Id),
            ("date" or "createdat", true) => users.OrderByDescending(u => u.CreatedAt).ThenBy(u => u.Id),
            (_, true) => users.OrderByDescending(u => u.FullName).ThenBy(u => u.Id),
            _ => users.OrderBy(u => u.FullName).ThenBy(u => u.Id)
        };

        var items = await ordered.Skip(query.Skip).Take(query.PageSize)
            .ProjectTo<UserSummaryDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return PagedResult<UserSummaryDto>.Create(items, total, query);
    }

    public async Task<UserSummaryDto> GetAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .ProjectTo<UserSummaryDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);

        return user ?? throw new NotFoundException("Kullanıcı", userId);
    }
}