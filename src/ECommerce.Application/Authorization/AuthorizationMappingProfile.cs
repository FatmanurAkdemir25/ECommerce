using AutoMapper;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Authorization;

public class AuthorizationMappingProfile : Profile
{
    public AuthorizationMappingProfile()
    {
        CreateMap<Role, RoleDto>();
        CreateMap<Role, RoleDetailDto>()
            .ForMember(d => d.Permissions, o => o.MapFrom(
                s => s.RolePermissions.Select(rp => rp.Permission.Name).OrderBy(n => n)));
        CreateMap<Permission, PermissionDto>();
    }
}