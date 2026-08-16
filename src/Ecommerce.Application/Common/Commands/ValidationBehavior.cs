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
            // Resolve any registered validators for this command
            var validators = _provider.GetService(typeof(System.Collections.Generic.IEnumerable<Ecommerce.Application.Common.Validation.IValidator<TCommand>>)) as System.Collections.Generic.IEnumerable<Ecommerce.Application.Common.Validation.IValidator<TCommand>>;

            if (validators != null)
            {
                var errors = new System.Collections.Generic.List<string>();
                foreach (var v in validators)
                {
                    var res = await v.ValidateAsync(command);
                    if (!res.IsValid) errors.AddRange(res.Errors);
                }

                if (errors.Count > 0)
                {
                    throw new Ecommerce.Domain.Exceptions.DomainException(string.Join("; ", errors));
                }
            }

            return await next();
        }
    }
}
