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
                .ForMember(d => d.Slug, opt => opt.MapFrom(s => s.Slug))
                .ForMember(d => d.Description, opt => opt.MapFrom(s => s.Description))
                .ForMember(d => d.IsActive, opt => opt.MapFrom(s => s.IsActive))
                .ForMember(d => d.Category, opt => opt.MapFrom(s => s.Category))
                .ForMember(d => d.Brand, opt => opt.MapFrom(s => s.Brand))
                .ForMember(d => d.AvailableStock, opt => opt.MapFrom(s => s.InventoryItems.Sum(i => i.Available)));

            CreateMap<Product, AdminProductDto>()
                .ForMember(d => d.Variants, opt => opt.MapFrom(s => s.Variants))
                .ForMember(d => d.Images, opt => opt.MapFrom(s => s.Images))
                .ForMember(d => d.Stock, opt => opt.MapFrom(s => s.InventoryItems.Sum(i => i.QuantityOnHand)))
                .ForMember(d => d.AvailableStock, opt => opt.MapFrom(s => s.InventoryItems.Sum(i => i.Available)))
                .ForMember(d => d.WarehouseId, opt => opt.MapFrom(s => s.InventoryItems.Select(i => (Guid?)i.WarehouseId).FirstOrDefault()))
                .ForMember(d => d.WarehouseName, opt => opt.MapFrom(s => s.InventoryItems.Select(i => i.Warehouse != null ? i.Warehouse.Name : string.Empty).FirstOrDefault() ?? string.Empty));

            CreateMap<ProductVariant, AdminProductVariantDto>();
            CreateMap<ProductImage, AdminProductImageDto>();
            CreateMap<ProductAttribute, AdminProductAttributeDto>();
            CreateMap<Coupon, AdminCouponDto>();
            CreateMap<Promotion, AdminPromotionDto>();

            CreateMap<Currency, CurrencyDto>();
            CreateMap<ExchangeRate, ExchangeRateDto>()
                .ForMember(d => d.FromCurrencyCode, opt => opt.Ignore())
                .ForMember(d => d.ToCurrencyCode, opt => opt.Ignore());

            CreateMap<Payment, AdminPaymentDto>();
            CreateMap<Refund, AdminRefundDto>();

            CreateMap<ShippingZone, AdminShippingZoneDto>();
            CreateMap<ShippingZoneLocation, AdminShippingZoneLocationDto>();
            CreateMap<ShippingMethod, AdminShippingMethodDto>();
            CreateMap<ShippingRate, AdminShippingRateDto>();

            CreateMap<TaxCategory, AdminTaxCategoryDto>();
            CreateMap<TaxRate, AdminTaxRateDto>();

            CreateMap<Notification, AdminNotificationDto>();
            CreateMap<NotificationTemplate, AdminNotificationTemplateDto>();
            CreateMap<NotificationPreference, AdminNotificationPreferenceDto>();
            CreateMap<NotificationChannel, AdminNotificationChannelDto>();

            CreateMap<Ecommerce.Domain.Entities.InventoryItem, AdminInventoryDto>()
                .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product != null ? s.Product.Name : string.Empty))
                .ForMember(d => d.VariantName, opt => opt.MapFrom(s => s.ProductVariant != null ? s.ProductVariant.Name : string.Empty))
                .ForMember(d => d.Sku, opt => opt.MapFrom(s => s.ProductVariant != null ? s.ProductVariant.Sku : (s.Product != null ? s.Product.Sku : string.Empty)))
                .ForMember(d => d.WarehouseName, opt => opt.MapFrom(s => s.Warehouse != null ? s.Warehouse.Name : string.Empty));

            // Cart mappings rely on convention (incl. enum->string for Status
            // and computed getters TotalAmount / LineTotal).
            CreateMap<Cart, CartDto>();
            CreateMap<CartItem, CartItemDto>();

            CreateMap<Category, CategoryDto>();
            CreateMap<Brand, BrandDto>();
            CreateMap<Warehouse, WarehouseDto>();

            CreateMap<ProductReview, ProductReviewDto>()
                .ForMember(d => d.UserDisplayName, opt => opt.Ignore());

            CreateMap<Shipment, ShipmentDto>()
                .ForMember(d => d.WarehouseName, opt => opt.Ignore())
                .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items));
            CreateMap<ShipmentItem, ShipmentItemDto>();

            CreateMap<SupportTicket, SupportTicketDto>()
                .ForMember(d => d.Messages, opt => opt.MapFrom(s => s.Messages));
            CreateMap<SupportTicketMessage, SupportTicketMessageDto>();

            CreateMap<Tag, TagDto>();
            CreateMap<Vendor, VendorDto>();

            CreateMap<VendorProduct, VendorProductDto>()
                .ForMember(d => d.VendorName, opt => opt.Ignore())
                .ForMember(d => d.ProductName, opt => opt.Ignore());

            CreateMap<Address, AddressDto>();
            CreateMap<AuditLog, AuditLogDto>();
    }
    }
}
