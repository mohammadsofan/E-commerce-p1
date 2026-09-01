using System.Threading.Tasks;
using Ecommerce.Application.Common.Validation;

namespace Ecommerce.Application.Commands.Checkout
{
    public class CheckoutCommandValidator : IValidator<CheckoutCommand>
    {
        public Task<ValidationResult> ValidateAsync(CheckoutCommand instance)
        {
            var result = new ValidationResult();

            if (instance.Items != null)
            {
                foreach (var it in instance.Items)
                {
                    if (it.Quantity <= 0)
                    {
                        result.IsValid = false;
                        result.Errors.Add("Quantity must be greater than zero for all items.");
                        break;
                    }
                }
            }

            return Task.FromResult(result);
        }
    }
}
