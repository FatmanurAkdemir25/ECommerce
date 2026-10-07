using AutoMapper;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Catalog;

public class CatalogMappingProfile : Profile
{
    public CatalogMappingProfile()
    {
        CreateMap<Category, CategoryDto>();

        CreateMap<Product, ProductDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name));

        CreateMap<ProductTransaction, ProductTransactionDto>()
            .ForMember(d => d.Source, o => o.MapFrom(s => s.Receipt.Source))
            .ForMember(d => d.OrderId, o => o.MapFrom(s => s.Receipt.OrderId))
            .ForMember(d => d.PerformedByUserId, o => o.MapFrom(s => s.Receipt.PerformedByUserId))
            .ForMember(d => d.Note, o => o.MapFrom(s => s.Receipt.Note));
    }
}