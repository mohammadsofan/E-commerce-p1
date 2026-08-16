using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Common.Queries
{
    public class QueryDispatcher
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<QueryDispatcher> _logger;

        public QueryDispatcher(IServiceProvider provider, ILogger<QueryDispatcher> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        public async Task<TResult> Send<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
            where TQuery : class
        {
            _logger.LogInformation("Dispatching query {QueryType}", typeof(TQuery).FullName);

            var handler = _provider.GetService<IQueryHandler<TQuery, TResult>>();
            if (handler == null) throw new InvalidOperationException($"No handler registered for {typeof(TQuery).FullName}");

            var result = await handler.Handle(query, cancellationToken);

            _logger.LogInformation("Query {QueryType} handled", typeof(TQuery).FullName);
            return result;
        }
    }
}
