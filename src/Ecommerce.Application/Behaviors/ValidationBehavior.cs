using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.Application.Behaviors
{
    // Placeholder for pipeline behavior (e.g., FluentValidation) when MediatR is used
    public class ValidationBehavior
    {
        public Task InvokeAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
