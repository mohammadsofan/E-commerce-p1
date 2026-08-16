using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Domain.DomainEvents;

namespace Ecommerce.Application.Common.DomainEvents
{
    public interface IDomainEventDispatcher
    {
        Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
    }
}
