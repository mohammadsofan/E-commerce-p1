using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Application.Interfaces;

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
                services.AddTransient<FluentValidation.IValidator<Ecommerce.Application.Commands.Checkout.CheckoutCommand>, Ecommerce.Application.Validators.CheckoutCommandFluentValidator>();
                services.AddTransient<FluentValidation.IValidator<Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommand>, Ecommerce.Application.Validators.ReserveInventoryFluentValidator>();

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

            // Payment gateway (stub) - replace with real provider implementation in production
            services.AddScoped<Ecommerce.Application.Interfaces.IPaymentService, Ecommerce.Infrastructure.Payments.PaymentGateway>();

            // Idempotency service
            services.AddScoped<Ecommerce.Application.Interfaces.IIdempotencyService, Ecommerce.Infrastructure.Services.IdempotencyService>();

            // Token service (JWT)
            services.AddScoped<Ecommerce.Application.Interfaces.ITokenService, Ecommerce.Infrastructure.Auth.JwtTokenService>();

            return services;
        }
    }
}
