using AutoMapper;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Accounts;

public class AccountMappingProfile : Profile
{
    public AccountMappingProfile()
    {
        CreateMap<User, ProfileDto>();
        CreateMap<User, UserSummaryDto>()
            .ForMember(d => d.Roles, o => o.MapFrom(
                s => s.UserRoles.Select(ur => ur.Role.Name).OrderBy(n => n)));
        CreateMap<Address, AddressDto>();
    }
}