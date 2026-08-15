using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.Application.Common.Commands
{
    public interface ICommandHandler<TCommand, TResult> where TCommand : class
    {
        Task<TResult> Handle(TCommand command, CancellationToken cancellationToken = default);
    }
}
