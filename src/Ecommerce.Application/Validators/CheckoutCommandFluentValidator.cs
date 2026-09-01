using FluentValidation;

namespace Ecommerce.Application.Commands.Checkout
{
    public class CheckoutCommandFluentValidator : AbstractValidator<CheckoutCommand>
    {
        public CheckoutCommandFluentValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            // Items is intentionally NOT required to be non-empty at the API boundary.
            // When Items is null or empty, CheckoutCommandHandler reads from the authenticated
            // user's server-side cart, which is the secure source of truth.
            // The handler will throw a DomainException if both the payload and cart are empty.
            RuleForEach(x => x.Items).ChildRules(items =>
            {
                items.RuleFor(i => i.Quantity).GreaterThan(0);
            });
            RuleFor(x => x.Currency).NotEmpty().When(x => x.Currency != null);
        }
    }
}
