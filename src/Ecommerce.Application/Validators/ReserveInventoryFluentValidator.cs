using FluentValidation;

namespace Ecommerce.Application.Commands.ReserveInventory
{
    public class ReserveInventoryFluentValidator : AbstractValidator<ReserveInventoryCommand>
    {
        public ReserveInventoryFluentValidator()
        {
            RuleFor(x => x.InventoryItemId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0);
        }
    }
}
