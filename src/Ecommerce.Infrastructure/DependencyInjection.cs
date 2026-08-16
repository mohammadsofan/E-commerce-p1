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

            // Admin dashboard query handler
            services.AddScoped<IQueryHandler<GetAdminDashboardQuery, AdminDashboardDto>, GetAdminDashboardQueryHandler>();

            // Payment gateway - use Stripe provider (configured via appsettings.json)
            services.Configure<StripeOptions>(configuration.GetSection("Stripe"));
            services.AddScoped<Ecommerce.Application.Interfaces.IPaymentService, Ecommerce.Infrastructure.Payments.StripePaymentProvider>();

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

            // Register EF configurations for new entities
            // (Applied automatically via ApplyConfigurationsFromAssembly)

            return services;
        }
    }
}
