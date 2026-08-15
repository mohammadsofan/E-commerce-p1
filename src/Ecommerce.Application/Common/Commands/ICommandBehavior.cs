using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.Application.Common.Commands
{
    public interface ICommandBehavior<TCommand, TResult>
    {
        Task<TResult> Handle(TCommand command, Func<Task<TResult>> next, CancellationToken cancellationToken = default);
    }
}
