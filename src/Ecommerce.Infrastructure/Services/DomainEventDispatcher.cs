using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common.DomainEvents;
using Ecommerce.Domain.DomainEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Services
{
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<DomainEventDispatcher> _logger;

        public DomainEventDispatcher(IServiceProvider provider, ILogger<DomainEventDispatcher> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            foreach (var domainEvent in domainEvents)
            {
                var eventType = domainEvent.GetType();
                var handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
                var handlersType = typeof(IEnumerable<>).MakeGenericType(handlerInterface);

                var handlers = (_provider.GetService(handlersType) as IEnumerable) ?? Array.Empty<object>();

                foreach (var handler in handlers)
                {
                    var handleMethod = handler.GetType().GetMethod("Handle", new[] { eventType, typeof(CancellationToken) });
                    if (handleMethod is null) continue;

                    _logger.LogInformation("Dispatching {DomainEvent} to {Handler}", eventType.Name, handler.GetType().Name);

                    var task = (Task?)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken });
                    if (task is not null) await task;
                }
            }
        }
    }
}
