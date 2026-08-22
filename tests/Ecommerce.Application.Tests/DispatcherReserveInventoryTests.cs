using System;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.ReserveInventory;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ecommerce.Application.Common;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class DispatcherReserveInventoryTests
    {
        private ServiceProvider BuildServiceProvider(ApplicationDbContext context)
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddSingleton(context);
            services.AddSingleton<Ecommerce.Application.Interfaces.IApplicationDbContext>(context);
            services.AddScoped<ICommandHandler<ReserveInventoryCommand, Unit>, ReserveInventoryCommandHandler>();
            services.AddScoped<CommandDispatcher>();

            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task Dispatcher_Invokes_Handler()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var item = new InventoryItem { Id = Guid.NewGuid() };
            item.AddStock(8);
            await context.InventoryItems.AddAsync(item);
            await context.SaveChangesAsync();

            var provider = BuildServiceProvider(context);

            var dispatcher = provider.GetRequiredService<CommandDispatcher>();

            var command = new ReserveInventoryCommand { InventoryItemId = item.Id, Quantity = 3 };

            await dispatcher.Send<ReserveInventoryCommand, Unit>(command);

            var updated = await context.InventoryItems.FirstAsync(i => i.Id == item.Id);
            Assert.Equal(3, updated.QuantityReserved);
        }
    }

    
}

