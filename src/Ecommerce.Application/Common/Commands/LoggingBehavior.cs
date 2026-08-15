using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Common.Commands
{
    public class LoggingBehavior<TCommand, TResult> : ICommandBehavior<TCommand, TResult>
    {
        private readonly ILogger<LoggingBehavior<TCommand, TResult>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TCommand, TResult>> logger)
        {
            _logger = logger;
        }

        public async Task<TResult> Handle(TCommand command, Func<Task<TResult>> next, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Handling {Command}", typeof(TCommand).FullName);
            try
            {
                var result = await next();
                _logger.LogInformation("Handled {Command}", typeof(TCommand).FullName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling {Command}", typeof(TCommand).FullName);
                throw;
            }
        }
    }
}
