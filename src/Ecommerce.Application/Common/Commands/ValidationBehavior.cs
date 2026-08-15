using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Application.Common.Commands
{
    public class ValidationBehavior<TCommand, TResult> : ICommandBehavior<TCommand, TResult>
    {
        private readonly IServiceProvider _provider;

        public ValidationBehavior(IServiceProvider provider)
        {
            _provider = provider;
        }

        public async Task<TResult> Handle(TCommand command, Func<Task<TResult>> next, CancellationToken cancellationToken = default)
        {
            // If FluentValidation validators are registered, they can be resolved and executed here.
            // This placeholder ensures the pipeline has a validation hook.

            // Example (if using FluentValidation):
            // var validators = _provider.GetServices<IValidator<TCommand>>();
            // foreach(var v in validators) { var result = await v.ValidateAsync(command); if (!result.IsValid) throw new ValidationException(result.Errors); }

            return await next();
        }
    }
}
