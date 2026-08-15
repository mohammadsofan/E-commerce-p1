using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // Register DbContext, Identity, repositories, services, etc.
            // Example (in real code): services.AddDbContext<ApplicationDbContext>(options => ...);

            return services;
        }
    }
}
