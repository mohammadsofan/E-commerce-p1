using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Common.Commands
{
    public class CommandDispatcher
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<CommandDispatcher> _logger;

        public CommandDispatcher(IServiceProvider provider, ILogger<CommandDispatcher> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        public async Task<TResult> Send<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class
        {
            _logger.LogInformation("Dispatching command {CommandType}", typeof(TCommand).FullName);

            var handler = _provider.GetService<ICommandHandler<TCommand, TResult>>();
            if (handler == null) throw new InvalidOperationException($"No handler registered for {typeof(TCommand).FullName}");

            // resolve pipeline behaviors
            var behaviors = (IEnumerable<ICommandBehavior<TCommand, TResult>>)_provider.GetService(typeof(IEnumerable<ICommandBehavior<TCommand, TResult>>))
                            ?? Array.Empty<ICommandBehavior<TCommand, TResult>>();

            // build pipeline
            Func<Task<TResult>> handlerDelegate = () => handler.Handle(command, cancellationToken);

            Func<Task<TResult>> pipeline = behaviors.Reverse().Aggregate(handlerDelegate, (next, behavior) =>
            {
                return () => behavior.Handle(command, next, cancellationToken);
            });

            var result = await pipeline();

            _logger.LogInformation("Command {CommandType} handled", typeof(TCommand).FullName);
            return result;
        }
    }
}
