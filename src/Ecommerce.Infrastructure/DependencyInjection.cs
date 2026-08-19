using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.Commands.Orders;
using Ecommerce.Application.Commands.Carts;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Products;
using Ecommerce.Application.Queries.Orders;
using Ecommerce.Application.Queries.Carts;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Infrastructure.Services;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Ecommerce.Infrastructure.Payments;
using Microsoft.AspNetCore.Identity;
using Ecommerce.Infrastructure.Identity;
using Ecommerce.Infrastructure.Services;

namespace Ecommerce.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Register ApplicationDbContext. Caller should ensure the correct EF provider package is referenced.
            // Example connection string name: "DefaultConnection"
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                var conn = configuration.GetConnectionString("DefaultConnection");
                // Default to SQL Server; change as needed. Requires Microsoft.EntityFrameworkCore.SqlServer package.
                options.UseSqlServer(conn);
            });

            // Expose interface for Application layer
            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

            // TODO: register repositories, identity services, event dispatchers, etc.

            // Register application command dispatcher and pipeline behaviors
            services.AddScoped<Ecommerce.Application.Common.Commands.CommandDispatcher>();
            services.AddScoped(typeof(Ecommerce.Application.Common.Commands.ICommandBehavior<,>), typeof(Ecommerce.Application.Common.Commands.LoggingBehavior<,>));
            services.AddScoped(typeof(Ecommerce.Application.Common.Commands.ICommandBehavior<,>), typeof(Ecommerce.Application.Common.Commands.ValidationBehavior<,>));

            // Register validators (application specific)
            services.AddScoped<Ecommerce.Application.Common.Validation.IValidator<Ecommerce.Application.Commands.Checkout.CheckoutCommand>, Ecommerce.Application.Commands.Checkout.CheckoutCommandValidator>();

            // Attempt to register FluentValidation validators and adapter if FluentValidation is available
            try
            {
                // Register Fluent validators
                services.AddTransient<FluentValidation.IValidator<Ecommerce.Application.Commands.Checkout.CheckoutCommand>, Ecommerce.Application.Commands.Checkout.CheckoutCommandFluentValidator>();
                services.AddTransient<FluentValidation.IValidator<Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommand>, Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryFluentValidator>();

                // Register adapter to expose Fluent validators as the application's IValidator<T>
                services.AddTransient<Ecommerce.Application.Common.Validation.IValidator<Ecommerce.Application.Commands.Checkout.CheckoutCommand>>(sp =>
                    new Ecommerce.Application.Common.Validation.FluentValidationAdapter<Ecommerce.Application.Commands.Checkout.CheckoutCommand>(sp.GetRequiredService<FluentValidation.IValidator<Ecommerce.Application.Commands.Checkout.CheckoutCommand>>()));

                services.AddTransient<Ecommerce.Application.Common.Validation.IValidator<Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommand>>(sp =>
                    new Ecommerce.Application.Common.Validation.FluentValidationAdapter<Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommand>(sp.GetRequiredService<FluentValidation.IValidator<Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommand>>()));
            }
            catch
            {
                // FluentValidation package not installed — DI of Fluent validators skipped.
            }

            // Register AutoMapper profiles (application mappings)
            // Requires AutoMapper & AutoMapper.Extensions.Microsoft.DependencyInjection packages
            try
            {
                services.AddAutoMapper(typeof(Ecommerce.Application.Mappings.MappingProfile), typeof(Ecommerce.Infrastructure.Mappings.AdminUserMappingProfile));
            }
            catch
            {
                // If AutoMapper package is not available yet, skip registration. Add package and restore locally.
            }

            // Register application command handlers
            services.AddScoped<Ecommerce.Application.Common.Commands.ICommandHandler<Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommand, Ecommerce.Application.Common.Unit>, Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommandHandler>();
            services.AddScoped<Ecommerce.Application.Common.Commands.ICommandHandler<Ecommerce.Application.Commands.Checkout.CheckoutCommand, System.Guid>, Ecommerce.Application.Commands.Checkout.CheckoutCommandHandler>();
            // Note: CheckoutCommandHandler requires IIdempotencyService; registration done below

            // Order lifecycle command handlers (MarkPaid / Cancel / Complete)
            services.AddScoped<ICommandHandler<MarkOrderPaidCommand, OrderDto>, MarkOrderPaidCommandHandler>();
            services.AddScoped<ICommandHandler<CancelOrderCommand, OrderDto>, CancelOrderCommandHandler>();
            services.AddScoped<ICommandHandler<CompleteOrderCommand, OrderDto>, CompleteOrderCommandHandler>();

            // Cart command handlers
            services.AddScoped<ICommandHandler<AddToCartCommand, CartDto>, AddToCartCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateCartItemCommand, CartDto>, UpdateCartItemCommandHandler>();
            services.AddScoped<ICommandHandler<RemoveFromCartCommand, CartDto>, RemoveFromCartCommandHandler>();
            services.AddScoped<ICommandHandler<ClearCartCommand, CartDto>, ClearCartCommandHandler>();

            // Admin product command handlers
            services.AddScoped<ICommandHandler<CreateProductCommand, AdminProductDto>, CreateProductCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateProductCommand, AdminProductDto>, UpdateProductCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteProductCommand, Unit>, DeleteProductCommandHandler>();

            // Admin order command handlers
            services.AddScoped<ICommandHandler<MarkOrderShippedCommand, Unit>, MarkOrderShippedCommandHandler>();
            services.AddScoped<ICommandHandler<MarkOrderDeliveredCommand, Unit>, MarkOrderDeliveredCommandHandler>();
            services.AddScoped<ICommandHandler<ProcessOrderRefundCommand, Unit>, ProcessOrderRefundCommandHandler>();
            services.AddScoped<ICommandHandler<ProcessOrderReturnCommand, Unit>, ProcessOrderReturnCommandHandler>();

            // Admin user command handlers
            services.AddScoped<ICommandHandler<Ecommerce.Application.Commands.Admin.CreateUserCommand, AdminUserDto>, CreateUserCommandHandler>();
            services.AddScoped<ICommandHandler<Ecommerce.Application.Commands.Admin.UpdateUserCommand, AdminUserDto>, UpdateUserCommandHandler>();
            services.AddScoped<ICommandHandler<Ecommerce.Application.Commands.Admin.DeleteUserCommand, Unit>, DeleteUserCommandHandler>();
            services.AddScoped<ICommandHandler<Ecommerce.Application.Commands.Admin.ChangePasswordCommand, Unit>, ChangePasswordCommandHandler>();
            services.AddScoped<ICommandHandler<Ecommerce.Application.Commands.Admin.SetUserRolesCommand, Unit>, SetUserRolesCommandHandler>();

            // Admin inventory command handlers
            services.AddScoped<ICommandHandler<Ecommerce.Application.Commands.Admin.AdjustInventoryCommand, Unit>, AdjustInventoryCommandHandler>();
            services.AddScoped<ICommandHandler<Ecommerce.Application.Commands.Admin.TransferInventoryCommand, Unit>, TransferInventoryCommandHandler>();
            services.AddScoped<ICommandHandler<Ecommerce.Application.Commands.Admin.SetReorderPointCommand, Unit>, SetReorderPointCommandHandler>();

            // Admin product variant command handlers
            services.AddScoped<ICommandHandler<CreateProductVariantCommand, AdminProductVariantDto>, CreateProductVariantCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateProductVariantCommand, AdminProductVariantDto>, UpdateProductVariantCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteProductVariantCommand, Unit>, DeleteProductVariantCommandHandler>();

            // Admin product attribute command handlers
            services.AddScoped<ICommandHandler<CreateProductAttributeCommand, AdminProductAttributeDto>, CreateProductAttributeCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateProductAttributeCommand, AdminProductAttributeDto>, UpdateProductAttributeCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteProductAttributeCommand, Unit>, DeleteProductAttributeCommandHandler>();

            // Admin coupon command handlers
            services.AddScoped<ICommandHandler<CreateCouponCommand, AdminCouponDto>, CreateCouponCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateCouponCommand, AdminCouponDto>, UpdateCouponCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteCouponCommand, Unit>, DeleteCouponCommandHandler>();

            // Admin currency command handlers
            services.AddScoped<ICommandHandler<CreateCurrencyCommand, CurrencyDto>, CreateCurrencyCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateCurrencyCommand, CurrencyDto>, UpdateCurrencyCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteCurrencyCommand, Unit>, DeleteCurrencyCommandHandler>();
            services.AddScoped<ICommandHandler<CreateExchangeRateCommand, ExchangeRateDto>, CreateExchangeRateCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateExchangeRateCommand, ExchangeRateDto>, UpdateExchangeRateCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteExchangeRateCommand, Unit>, DeleteExchangeRateCommandHandler>();

            // Admin promotion command handlers
            services.AddScoped<ICommandHandler<CreatePromotionCommand, AdminPromotionDto>, CreatePromotionCommandHandler>();
            services.AddScoped<ICommandHandler<UpdatePromotionCommand, AdminPromotionDto>, UpdatePromotionCommandHandler>();
            services.AddScoped<ICommandHandler<DeletePromotionCommand, Unit>, DeletePromotionCommandHandler>();

            // Admin payment command handlers
            services.AddScoped<ICommandHandler<CapturePaymentCommand, PaymentResultDto>, CapturePaymentCommandHandler>();
            services.AddScoped<ICommandHandler<VoidPaymentCommand, PaymentResultDto>, VoidPaymentCommandHandler>();
            services.AddScoped<ICommandHandler<RefundPaymentCommand, RefundResultDto>, RefundPaymentCommandHandler>();

            // Admin shipping command handlers
            services.AddScoped<ICommandHandler<CreateShippingZoneCommand, AdminShippingZoneDto>, CreateShippingZoneCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateShippingZoneCommand, AdminShippingZoneDto>, UpdateShippingZoneCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteShippingZoneCommand, Unit>, DeleteShippingZoneCommandHandler>();
            services.AddScoped<ICommandHandler<CreateShippingMethodCommand, AdminShippingMethodDto>, CreateShippingMethodCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateShippingMethodCommand, AdminShippingMethodDto>, UpdateShippingMethodCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteShippingMethodCommand, Unit>, DeleteShippingMethodCommandHandler>();
            services.AddScoped<ICommandHandler<CreateShippingRateOnlyCommand, AdminShippingRateDto>, CreateShippingRateOnlyCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateShippingRateOnlyCommand, AdminShippingRateDto>, UpdateShippingRateOnlyCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteShippingRateCommand, Unit>, DeleteShippingRateCommandHandler>();

            // Admin tax command handlers
            services.AddScoped<ICommandHandler<CreateTaxCategoryCommand, AdminTaxCategoryDto>, CreateTaxCategoryCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateTaxCategoryCommand, AdminTaxCategoryDto>, UpdateTaxCategoryCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteTaxCategoryCommand, Unit>, DeleteTaxCategoryCommandHandler>();
            services.AddScoped<ICommandHandler<CreateTaxRateOnlyCommand, AdminTaxRateDto>, CreateTaxRateOnlyCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateTaxRateOnlyCommand, AdminTaxRateDto>, UpdateTaxRateOnlyCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteTaxRateCommand, Unit>, DeleteTaxRateCommandHandler>();

            // Admin notification command handlers
            services.AddScoped<ICommandHandler<CreateNotificationCommand, AdminNotificationDto>, CreateNotificationCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateNotificationCommand, AdminNotificationDto>, UpdateNotificationCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteNotificationCommand, Unit>, DeleteNotificationCommandHandler>();
            services.AddScoped<ICommandHandler<CreateNotificationTemplateCommand, AdminNotificationTemplateDto>, CreateNotificationTemplateCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateNotificationTemplateCommand, AdminNotificationTemplateDto>, UpdateNotificationTemplateCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteNotificationTemplateCommand, Unit>, DeleteNotificationTemplateCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateNotificationPreferenceCommand, AdminNotificationPreferenceDto>, UpdateNotificationPreferenceCommandHandler>();
            services.AddScoped<ICommandHandler<CreateNotificationChannelCommand, AdminNotificationChannelDto>, CreateNotificationChannelCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateNotificationChannelCommand, AdminNotificationChannelDto>, UpdateNotificationChannelCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteNotificationChannelCommand, Unit>, DeleteNotificationChannelCommandHandler>();

            // Register query dispatcher and query handlers
            services.AddScoped<QueryDispatcher>();
            services.AddScoped<IQueryHandler<GetProductsQuery, List<ProductDto>>, GetProductsQueryHandler>();
            services.AddScoped<IQueryHandler<GetProductByIdQuery, ProductDto>, GetProductByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetProductBySlugQuery, ProductDto>, GetProductBySlugQueryHandler>();
            services.AddScoped<IQueryHandler<GetOrdersQuery, List<OrderDto>>, GetOrdersQueryHandler>();
            services.AddScoped<IQueryHandler<GetOrderByIdQuery, OrderDto>, GetOrderByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetCartQuery, CartDto>, GetCartQueryHandler>();

            // Admin product query handlers
            services.AddScoped<IQueryHandler<GetAdminProductsQuery, PagedResult<AdminProductDto>>, GetAdminProductsQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminProductByIdQuery, AdminProductDto>, GetAdminProductByIdQueryHandler>();

            // Admin order query handlers
            services.AddScoped<IQueryHandler<GetAdminOrdersQuery, PagedResult<OrderDto>>, GetAdminOrdersQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminOrderByIdQuery, OrderDto>, GetAdminOrderByIdQueryHandler>();

            // Admin user query handlers
            services.AddScoped<IQueryHandler<GetAdminUsersQuery, PagedResult<AdminUserDto>>, GetAdminUsersQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminUserByIdQuery, AdminUserDto>, GetAdminUserByIdQueryHandler>();

            // Admin inventory query handlers
            services.AddScoped<IQueryHandler<GetAdminInventoryQuery, PagedResult<AdminInventoryDto>>, GetAdminInventoryQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminInventoryByIdQuery, AdminInventoryDto>, GetAdminInventoryByIdQueryHandler>();

            // Admin product variant query handlers
            services.AddScoped<IQueryHandler<GetAdminProductVariantsQuery, PagedResult<AdminProductVariantDto>>, GetAdminProductVariantsQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminProductVariantByIdQuery, AdminProductVariantDto>, GetAdminProductVariantByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminProductImagesQuery, PagedResult<AdminProductImageDto>>, GetAdminProductImagesQueryHandler>();

            // Admin product attribute query handlers
            services.AddScoped<IQueryHandler<GetAdminProductAttributesQuery, PagedResult<AdminProductAttributeDto>>, GetAdminProductAttributesQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminProductAttributeByIdQuery, AdminProductAttributeDto>, GetAdminProductAttributeByIdQueryHandler>();

            // Admin coupon query handlers
            services.AddScoped<IQueryHandler<GetAdminCouponsQuery, PagedResult<AdminCouponDto>>, GetAdminCouponsQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminCouponByIdQuery, AdminCouponDto>, GetAdminCouponByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminCouponByCodeQuery, AdminCouponDto>, GetAdminCouponByCodeQueryHandler>();

            // Admin promotion query handlers
            services.AddScoped<IQueryHandler<GetAdminPromotionsQuery, PagedResult<AdminPromotionDto>>, GetAdminPromotionsQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminPromotionByIdQuery, AdminPromotionDto>, GetAdminPromotionByIdQueryHandler>();

            // Discount calculation query handlers
            services.AddScoped<IQueryHandler<ValidateCouponQuery, ValidateCouponResponse>, ValidateCouponQueryHandler>();
            services.AddScoped<IQueryHandler<CalculateDiscountsQuery, DiscountCalculationResult>, CalculateDiscountsQueryHandler>();

            // Admin payment query handlers
            services.AddScoped<IQueryHandler<GetAdminPaymentsQuery, PagedResult<AdminPaymentDto>>, GetAdminPaymentsQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminPaymentByIdQuery, AdminPaymentDto>, GetAdminPaymentByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminRefundsQuery, PagedResult<AdminRefundDto>>, GetAdminRefundsQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminRefundByIdQuery, AdminRefundDto>, GetAdminRefundByIdQueryHandler>();

            // Admin shipping query handlers
            services.AddScoped<IQueryHandler<GetAdminShippingZonesQuery, PagedResult<AdminShippingZoneDto>>, GetAdminShippingZonesQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminShippingZoneByIdQuery, AdminShippingZoneDto>, GetAdminShippingZoneByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminShippingMethodsQuery, PagedResult<AdminShippingMethodDto>>, GetAdminShippingMethodsQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminShippingMethodByIdQuery, AdminShippingMethodDto>, GetAdminShippingMethodByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminShippingRatesQuery, PagedResult<AdminShippingRateDto>>, GetAdminShippingRatesQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminShippingRateByIdQuery, AdminShippingRateDto>, GetAdminShippingRateByIdQueryHandler>();

            // Admin tax query handlers
            services.AddScoped<IQueryHandler<GetAdminTaxCategoriesQuery, PagedResult<AdminTaxCategoryDto>>, GetAdminTaxCategoriesQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminTaxCategoryByIdQuery, AdminTaxCategoryDto>, GetAdminTaxCategoryByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminTaxRatesQuery, PagedResult<AdminTaxRateDto>>, GetAdminTaxRatesQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminTaxRateByIdQuery, AdminTaxRateDto>, GetAdminTaxRateByIdQueryHandler>();

            // Currency & exchange rate query handlers
            services.AddScoped<IQueryHandler<GetCurrenciesQuery, List<CurrencyDto>>, GetCurrenciesQueryHandler>();
            services.AddScoped<IQueryHandler<GetExchangeRatesQuery, List<ExchangeRateDto>>, GetExchangeRatesQueryHandler>();
            services.AddScoped<IQueryHandler<ConvertCurrencyQuery, CurrencyConversionResult>, ConvertCurrencyQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminCurrenciesQuery, PagedResult<CurrencyDto>>, GetAdminCurrenciesQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminCurrencyByIdQuery, CurrencyDto>, GetAdminCurrencyByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminExchangeRatesQuery, PagedResult<ExchangeRateDto>>, GetAdminExchangeRatesQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminExchangeRateByIdQuery, ExchangeRateDto>, GetAdminExchangeRateByIdQueryHandler>();

            // Admin notification query handlers
            services.AddScoped<IQueryHandler<GetAdminNotificationsQuery, PagedResult<AdminNotificationDto>>, GetAdminNotificationsQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminNotificationByIdQuery, AdminNotificationDto>, GetAdminNotificationByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminNotificationTemplatesQuery, PagedResult<AdminNotificationTemplateDto>>, GetAdminNotificationTemplatesQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminNotificationTemplateByIdQuery, AdminNotificationTemplateDto>, GetAdminNotificationTemplateByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminNotificationPreferencesQuery, PagedResult<AdminNotificationPreferenceDto>>, GetAdminNotificationPreferencesQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminNotificationChannelsQuery, PagedResult<AdminNotificationChannelDto>>, GetAdminNotificationChannelsQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminNotificationChannelByIdQuery, AdminNotificationChannelDto>, GetAdminNotificationChannelByIdQueryHandler>();

            // Admin report query handlers
            services.AddScoped<IQueryHandler<GetSalesReportQuery, SalesReportDto>, GetSalesReportQueryHandler>();
            services.AddScoped<IQueryHandler<GetRevenueReportQuery, RevenueReportDto>, GetRevenueReportQueryHandler>();
            services.AddScoped<IQueryHandler<GetInventoryReportQuery, InventoryReportDto>, GetInventoryReportQueryHandler>();
            services.AddScoped<IQueryHandler<GetCustomerReportQuery, CustomerReportDto>, GetCustomerReportQueryHandler>();
            services.AddScoped<IQueryHandler<ExportReportQuery, ExportResult>, ExportReportQueryHandler>();

            // Admin dashboard query handler
            services.AddScoped<IQueryHandler<GetAdminDashboardQuery, AdminDashboardDto>, GetAdminDashboardQueryHandler>();

            // Catalog query handlers (categories / brands)
            services.AddScoped<IQueryHandler<GetCategoriesQuery, List<CategoryDto>>, GetCategoriesQueryHandler>();
            services.AddScoped<IQueryHandler<GetCategoryBySlugQuery, CategoryDto>, GetCategoryBySlugQueryHandler>();
            services.AddScoped<IQueryHandler<GetBrandsQuery, List<BrandDto>>, GetBrandsQueryHandler>();

            // Warehouse query + command handlers
            services.AddScoped<IQueryHandler<GetAdminWarehousesQuery, PagedResult<WarehouseDto>>, GetAdminWarehousesQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminWarehouseByIdQuery, WarehouseDto>, GetAdminWarehouseByIdQueryHandler>();
            services.AddScoped<ICommandHandler<CreateWarehouseCommand, WarehouseDto>, CreateWarehouseCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateWarehouseCommand, WarehouseDto>, UpdateWarehouseCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteWarehouseCommand, Unit>, DeleteWarehouseCommandHandler>();

            // Review query + command handlers
            services.AddScoped<IQueryHandler<GetProductReviewsQuery, List<ProductReviewDto>>, GetProductReviewsQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminReviewsQuery, PagedResult<ProductReviewDto>>, GetAdminReviewsQueryHandler>();
            services.AddScoped<ICommandHandler<SubmitProductReviewCommand, ProductReviewDto>, SubmitProductReviewCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateReviewStatusCommand, Unit>, UpdateReviewStatusCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteReviewCommand, Unit>, DeleteReviewCommandHandler>();

            // Shipment query + command handlers
            services.AddScoped<IQueryHandler<GetAdminShipmentsQuery, PagedResult<ShipmentDto>>, GetAdminShipmentsQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminShipmentByIdQuery, ShipmentDto>, GetAdminShipmentByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetOrderShipmentQuery, ShipmentDto>, GetOrderShipmentQueryHandler>();
            services.AddScoped<ICommandHandler<CreateShipmentCommand, ShipmentDto>, CreateShipmentCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateShipmentStatusCommand, Unit>, UpdateShipmentStatusCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateShipmentTrackingCommand, Unit>, UpdateShipmentTrackingCommandHandler>();

            // Support ticket query + command handlers
            services.AddScoped<IQueryHandler<GetMySupportTicketsQuery, List<SupportTicketDto>>, GetMySupportTicketsQueryHandler>();
            services.AddScoped<IQueryHandler<GetSupportTicketByIdQuery, SupportTicketDto>, GetSupportTicketByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminSupportTicketsQuery, PagedResult<SupportTicketDto>>, GetAdminSupportTicketsQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminSupportTicketByIdQuery, SupportTicketDto>, GetAdminSupportTicketByIdQueryHandler>();
            services.AddScoped<ICommandHandler<CreateSupportTicketCommand, SupportTicketDto>, CreateSupportTicketCommandHandler>();
            services.AddScoped<ICommandHandler<ReplySupportTicketCommand, Unit>, ReplySupportTicketCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateSupportTicketCommand, Unit>, UpdateSupportTicketCommandHandler>();

            // Tag query + command handlers
            services.AddScoped<IQueryHandler<GetTagsQuery, List<TagDto>>, GetTagsQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminTagsQuery, PagedResult<TagDto>>, GetAdminTagsQueryHandler>();
            services.AddScoped<ICommandHandler<CreateTagCommand, TagDto>, CreateTagCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateTagCommand, TagDto>, UpdateTagCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteTagCommand, Unit>, DeleteTagCommandHandler>();

            // Vendor query + command handlers
            services.AddScoped<IQueryHandler<GetAdminVendorsQuery, PagedResult<VendorDto>>, GetAdminVendorsQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminVendorByIdQuery, VendorDto>, GetAdminVendorByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetVendorProductsQuery, List<VendorProductDto>>, GetVendorProductsQueryHandler>();
            services.AddScoped<ICommandHandler<CreateVendorCommand, VendorDto>, CreateVendorCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateVendorCommand, VendorDto>, UpdateVendorCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteVendorCommand, Unit>, DeleteVendorCommandHandler>();
            services.AddScoped<ICommandHandler<CreateVendorProductCommand, VendorProductDto>, CreateVendorProductCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteVendorProductCommand, Unit>, DeleteVendorProductCommandHandler>();

            // Address query + command handlers
            services.AddScoped<IQueryHandler<GetMyAddressesQuery, List<AddressDto>>, GetMyAddressesQueryHandler>();
            services.AddScoped<IQueryHandler<GetAddressByIdQuery, AddressDto>, GetAddressByIdQueryHandler>();
            services.AddScoped<ICommandHandler<CreateAddressCommand, AddressDto>, CreateAddressCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateAddressCommand, AddressDto>, UpdateAddressCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteAddressCommand, Unit>, DeleteAddressCommandHandler>();

            // Profile query + command handlers
            services.AddScoped<IQueryHandler<GetMyProfileQuery, AdminUserDto>, Ecommerce.Infrastructure.Services.Profile.GetMyProfileQueryHandler>();
            services.AddScoped<ICommandHandler<UpdateProfileCommand, AdminUserDto>, Ecommerce.Infrastructure.Services.Profile.UpdateProfileCommandHandler>();

            // Audit log query handlers
            services.AddScoped<IQueryHandler<GetAdminAuditLogsQuery, PagedResult<AuditLogDto>>, GetAdminAuditLogsQueryHandler>();
            services.AddScoped<IQueryHandler<GetAdminAuditLogByIdQuery, AuditLogDto>, GetAdminAuditLogByIdQueryHandler>();

            // Payment gateway - use Stripe provider (configured via appsettings.json)
            services.Configure<Ecommerce.Infrastructure.Payments.StripePaymentProvider.StripeOptions>(configuration.GetSection("Stripe"));
            services.AddScoped(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Ecommerce.Infrastructure.Payments.StripePaymentProvider.StripeOptions>>().Value);
            services.AddScoped<Ecommerce.Application.Interfaces.IPaymentService, Ecommerce.Infrastructure.Payments.StripePaymentProvider>();
            services.AddScoped<Ecommerce.Application.Interfaces.IStripeWebhookService, Ecommerce.Infrastructure.Payments.StripeWebhookService>();

            // Idempotency service
            services.AddScoped<Ecommerce.Application.Interfaces.IIdempotencyService, Ecommerce.Infrastructure.Services.IdempotencyService>();

            // Refresh token service
            services.AddScoped<Ecommerce.Application.Interfaces.IRefreshTokenService, Ecommerce.Infrastructure.Services.RefreshTokenService>();

            // Token service (JWT)
            services.AddScoped<Ecommerce.Application.Interfaces.ITokenService, Ecommerce.Infrastructure.Auth.JwtTokenService>();

            // Domain event dispatching
            services.AddScoped<Ecommerce.Application.Common.DomainEvents.IDomainEventDispatcher, Ecommerce.Infrastructure.Services.DomainEventDispatcher>();
            services.AddScoped<Ecommerce.Application.Common.DomainEvents.IDomainEventHandler<Ecommerce.Domain.DomainEvents.OrderPlacedDomainEvent>, Ecommerce.Infrastructure.Services.OrderPlacedEventHandler>();

            // Hosted cleanup
            services.AddHostedService<Ecommerce.Infrastructure.Services.RefreshTokenCleanupService>();

            // Register database seeder
            services.AddTransient<Ecommerce.Infrastructure.Persistence.DbSeeder>();

            // Register user management service
            services.AddScoped<Ecommerce.Application.Interfaces.IUserManagementService, Ecommerce.Infrastructure.Services.UserManagementService>();

            // Email service (SMTP)
            services.Configure<Ecommerce.Infrastructure.Services.EmailOptions>(configuration.GetSection("Email"));
            services.AddScoped<Ecommerce.Application.Interfaces.IEmailService, Ecommerce.Infrastructure.Services.EmailService>();

            // SMS service
            services.Configure<Ecommerce.Infrastructure.Services.SmsOptions>(configuration.GetSection("Sms"));
            services.AddScoped<Ecommerce.Application.Interfaces.ISmsService, Ecommerce.Infrastructure.Services.SmsService>();

            // Push notification service
            services.Configure<Ecommerce.Infrastructure.Services.PushOptions>(configuration.GetSection("Push"));
            services.AddScoped<Ecommerce.Application.Interfaces.IPushNotificationService, Ecommerce.Infrastructure.Services.PushNotificationService>();

            // Product search index service
            services.AddScoped<Ecommerce.Application.Interfaces.IProductSearchService, Ecommerce.Infrastructure.Services.ProductSearchService>();

            // Register EF configurations for new entities
            // (Applied automatically via ApplyConfigurationsFromAssembly)

            return services;
        }
    }
}
