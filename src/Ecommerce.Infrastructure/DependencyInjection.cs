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

            // Register application command handlers
            services.AddScoped<Ecommerce.Application.Common.Commands.ICommandHandler<Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommand, Ecommerce.Application.Common.Unit>, Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommandHandler>();
            services.AddScoped<Ecommerce.Application.Common.Commands.ICommandHandler<Ecommerce.Application.Commands.Checkout.CheckoutCommand, System.Guid>, Ecommerce.Application.Commands.Checkout.CheckoutCommandHandler>();
            // Note: CheckoutCommandHandler requires IIdempotencyService; registration done below

            // Payment gateway (stub) - replace with real provider implementation in production
            services.AddScoped<Ecommerce.Application.Interfaces.IPaymentService, Ecommerce.Infrastructure.Payments.PaymentGateway>();

            // Idempotency service
            services.AddScoped<Ecommerce.Application.Interfaces.IIdempotencyService, Ecommerce.Infrastructure.Services.IdempotencyService>();

            return services;
        }
    }
}
