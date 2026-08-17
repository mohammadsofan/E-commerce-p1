using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Domain.DomainEvents;

namespace Ecommerce.Application.Common.DomainEvents
{
    public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        Task Handle(TEvent domainEvent, CancellationToken cancellationToken = default);
    }
}
