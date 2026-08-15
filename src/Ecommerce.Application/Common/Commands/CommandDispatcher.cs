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

            var result = await handler.Handle(command, cancellationToken);

            _logger.LogInformation("Command {CommandType} handled", typeof(TCommand).FullName);
            return result;
        }
    }
}
