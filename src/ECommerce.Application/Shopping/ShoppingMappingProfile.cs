using AutoMapper;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Shopping;

public class ShoppingMappingProfile : Profile
{
    public ShoppingMappingProfile()
    {
        CreateMap<CartItem, CartItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.UnitPrice, o => o.MapFrom(s => s.Product.Price))
            .ForMember(d => d.AvailableStock, o => o.MapFrom(s => s.Product.Stock))
            .ForMember(d => d.IsProductActive, o => o.MapFrom(s => s.Product.IsActive))
            .ForMember(d => d.Issue, o => o.Ignore()); // serviste hesaplanır
    }
}