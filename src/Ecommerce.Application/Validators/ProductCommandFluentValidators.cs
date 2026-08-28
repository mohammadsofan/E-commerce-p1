using FluentValidation;

namespace Ecommerce.Application.Validators
{
    /// <summary>
    /// Catalog pricing guards. A negative price used to be accepted here and only rejected
    /// later by <c>Order.AddItem</c>, i.e. after the product was already live and addable to
    /// a cart.
    /// </summary>
    public class CreateProductCommandFluentValidator : AbstractValidator<Commands.Admin.CreateProductCommand>
    {
        public CreateProductCommandFluentValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("اسم المنتج مطلوب.").MaximumLength(300);
            RuleFor(x => x.Slug).NotEmpty().WithMessage("الاسم اللطيف (slug) مطلوب.").MaximumLength(300);
            RuleFor(x => x.Sku).NotEmpty().WithMessage("رمز المنتج (SKU) مطلوب.").MaximumLength(100);

            RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0m).WithMessage("سعر المنتج لا يمكن أن يكون سالباً.");
            RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0m).WithMessage("سعر التكلفة لا يمكن أن يكون سالباً.");
            RuleFor(x => x.CompareAtPrice).GreaterThanOrEqualTo(0m).WithMessage("سعر المقارنة لا يمكن أن يكون سالباً.");
            RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).When(x => x.Stock.HasValue).WithMessage("الكمية لا يمكن أن تكون سالبة.");
        }
    }

    public class UpdateProductCommandFluentValidator : AbstractValidator<Commands.Admin.UpdateProductCommand>
    {
        public UpdateProductCommandFluentValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("معرّف المنتج مطلوب.");
            RuleFor(x => x.Name).NotEmpty().WithMessage("اسم المنتج مطلوب.").MaximumLength(300);
            RuleFor(x => x.Slug).NotEmpty().WithMessage("الاسم اللطيف (slug) مطلوب.").MaximumLength(300);
            RuleFor(x => x.Sku).NotEmpty().WithMessage("رمز المنتج (SKU) مطلوب.").MaximumLength(100);

            RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0m).WithMessage("سعر المنتج لا يمكن أن يكون سالباً.");
            RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0m).WithMessage("سعر التكلفة لا يمكن أن يكون سالباً.");
            RuleFor(x => x.CompareAtPrice).GreaterThanOrEqualTo(0m).WithMessage("سعر المقارنة لا يمكن أن يكون سالباً.");
        }
    }

    public class CreateProductVariantCommandFluentValidator : AbstractValidator<Commands.Admin.CreateProductVariantCommand>
    {
        public CreateProductVariantCommandFluentValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty().WithMessage("معرّف المنتج مطلوب.");
            RuleFor(x => x.Sku).NotEmpty().WithMessage("رمز الخيار (SKU) مطلوب.").MaximumLength(100);
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0m).WithMessage("سعر الخيار لا يمكن أن يكون سالباً.");
            RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0m).WithMessage("سعر التكلفة لا يمكن أن يكون سالباً.");
            RuleFor(x => x.CompareAtPrice).GreaterThanOrEqualTo(0m).WithMessage("سعر المقارنة لا يمكن أن يكون سالباً.");
        }
    }

    public class UpdateProductVariantCommandFluentValidator : AbstractValidator<Commands.Admin.UpdateProductVariantCommand>
    {
        public UpdateProductVariantCommandFluentValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("معرّف الخيار مطلوب.");
            RuleFor(x => x.Sku).NotEmpty().WithMessage("رمز الخيار (SKU) مطلوب.").MaximumLength(100);
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0m).WithMessage("سعر الخيار لا يمكن أن يكون سالباً.");
            RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0m).WithMessage("سعر التكلفة لا يمكن أن يكون سالباً.");
            RuleFor(x => x.CompareAtPrice).GreaterThanOrEqualTo(0m).WithMessage("سعر المقارنة لا يمكن أن يكون سالباً.");
        }
    }
}
