using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Domain.DomainEvents;

namespace Ecommerce.Application.Common.DomainEvents
{
    /// <summary>
    /// No-op dispatcher used when domain-event handling is not required (e.g. in unit tests).
    /// </summary>
    public sealed class NullDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
