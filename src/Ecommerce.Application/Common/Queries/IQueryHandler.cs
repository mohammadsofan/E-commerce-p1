using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.Application.Common.Queries
{
    public interface IQueryHandler<TQuery, TResult> where TQuery : class
    {
        Task<TResult> Handle(TQuery query, CancellationToken cancellationToken = default);
    }
}
