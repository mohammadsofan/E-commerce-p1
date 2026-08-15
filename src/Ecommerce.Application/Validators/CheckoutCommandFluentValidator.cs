using FluentValidation;

namespace Ecommerce.Application.Commands.Checkout
{
    public class CheckoutCommandFluentValidator : AbstractValidator<CheckoutCommand>
    {
        public CheckoutCommandFluentValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Items).NotEmpty().WithMessage("Cart must contain at least one item.");
            RuleForEach(x => x.Items).ChildRules(items =>
            {
                items.RuleFor(i => i.Quantity).GreaterThan(0);
            });
            RuleFor(x => x.Currency).NotEmpty().When(x => x.Currency != null);
        }
    }
}
