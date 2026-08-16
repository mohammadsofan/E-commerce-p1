using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common.DomainEvents;
using Ecommerce.Domain.DomainEvents;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Services
{
    /// <summary>
    /// Sample handler demonstrating domain-event wiring. Reacts to an order being placed.
    /// </summary>
    public class OrderPlacedEventHandler : IDomainEventHandler<OrderPlacedDomainEvent>
    {
        private readonly ILogger<OrderPlacedEventHandler> _logger;

        public OrderPlacedEventHandler(ILogger<OrderPlacedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(OrderPlacedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Order {OrderId} was placed.", domainEvent.OrderId);
            return Task.CompletedTask;
        }
    }
}
