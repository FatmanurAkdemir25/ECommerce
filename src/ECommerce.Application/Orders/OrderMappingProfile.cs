using AutoMapper;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Orders;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<OrderItem, OrderItemDto>()
            .ForCtorParam("ProductName", o => o.MapFrom(i => i.Product.Name))
            .ForCtorParam("LineTotal", o => o.MapFrom(i => i.UnitPrice * i.Quantity));

        CreateMap<Payment, PaymentDto>();

        CreateMap<Order, OrderDto>();

        CreateMap<Order, OrderSummaryDto>()
            .ForCtorParam("ItemCount", o => o.MapFrom(s => s.Items.Count))
            .ForCtorParam(
                "PaymentStatus",
                o => o.MapFrom(s =>
                    s.Payment == null
                        ? (PaymentStatus?)null
                        : s.Payment.Status));
    }
}