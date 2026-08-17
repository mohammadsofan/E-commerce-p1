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
                services.AddAutoMapper(typeof(Ecommerce.Application.Mappings.MappingProfile));
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

            // Register EF configurations for new entities
            // (Applied automatically via ApplyConfigurationsFromAssembly)

            return services;
        }
    }
}
