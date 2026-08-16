using AutoMapper;
using Ecommerce.Application.DTOs;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Order, OrderDto>()
                .ForMember(d => d.TotalAmount, opt => opt.MapFrom(s => s.TotalAmount))
                .ForMember(d => d.OrderNumber, opt => opt.MapFrom(s => s.OrderNumber))
                .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items));

            CreateMap<OrderItem, OrderItemDto>()
                .ForMember(d => d.ProductId, opt => opt.MapFrom(s => s.ProductId))
                .ForMember(d => d.ProductVariantId, opt => opt.MapFrom(s => s.ProductVariantId))
                .ForMember(d => d.Quantity, opt => opt.MapFrom(s => s.Quantity))
                .ForMember(d => d.UnitPrice, opt => opt.MapFrom(s => s.UnitPrice));

            CreateMap<Product, ProductDto>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name))
                .ForMember(d => d.BasePrice, opt => opt.MapFrom(s => s.BasePrice))
                .ForMember(d => d.Slug, opt => opt.MapFrom(s => s.Slug));

            CreateMap<Product, AdminProductDto>()
                .ForMember(d => d.Variants, opt => opt.MapFrom(s => s.Variants))
                .ForMember(d => d.Images, opt => opt.MapFrom(s => s.Images));

            CreateMap<ProductVariant, AdminProductVariantDto>();
            CreateMap<ProductImage, AdminProductImageDto>();

            CreateMap<Ecommerce.Domain.Entities.InventoryItem, AdminInventoryDto>()
                .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product != null ? s.Product.Name : string.Empty))
                .ForMember(d => d.VariantName, opt => opt.MapFrom(s => s.ProductVariant != null ? s.ProductVariant.Name : string.Empty))
                .ForMember(d => d.Sku, opt => opt.MapFrom(s => s.ProductVariant != null ? s.ProductVariant.Sku : (s.Product != null ? s.Product.Sku : string.Empty)))
                .ForMember(d => d.WarehouseName, opt => opt.MapFrom(s => s.Warehouse != null ? s.Warehouse.Name : string.Empty));

            // Cart mappings rely on convention (incl. enum->string for Status
            // and computed getters TotalAmount / LineTotal).
            CreateMap<Cart, CartDto>();
            CreateMap<CartItem, CartItemDto>();
        }
    }
}
