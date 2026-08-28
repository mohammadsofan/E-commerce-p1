using System;
using FluentValidation;

namespace Ecommerce.Application.Validators
{
    /// <summary>
    /// Shared rules for creating and updating a customer address. Without these an empty
    /// address could be persisted and then selected as a shipping destination at checkout.
    /// </summary>
    internal static class AddressRules
    {
        public static void Apply<T>(AbstractValidator<T> validator)
            where T : Commands.Admin.IAddressCommand
        {
            validator.RuleFor(x => x.AddressLine1)
                .NotEmpty().WithMessage("عنوان الشارع مطلوب.")
                .MaximumLength(200).WithMessage("عنوان الشارع طويل جداً.");

            validator.RuleFor(x => x.AddressLine2)
                .MaximumLength(200).WithMessage("تفاصيل العنوان الإضافية طويلة جداً.");

            validator.RuleFor(x => x.City)
                .NotEmpty().WithMessage("المدينة مطلوبة.")
                .MaximumLength(100).WithMessage("اسم المدينة طويل جداً.");

            validator.RuleFor(x => x.State)
                .MaximumLength(100).WithMessage("اسم المحافظة طويل جداً.");

            validator.RuleFor(x => x.PostalCode)
                .MaximumLength(20).WithMessage("الرمز البريدي طويل جداً.");

            validator.RuleFor(x => x.EffectiveCountryCode)
                .NotEmpty().WithMessage("الدولة مطلوبة.")
                .Must(code => code != null && code.Trim().Length is >= 2 and <= 3)
                .WithMessage("رمز الدولة غير صحيح.");

            validator.RuleFor(x => x.PhoneNumber)
                .Matches(@"^[0-9+\-\s()]{7,20}$")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
                .WithMessage("رقم الهاتف غير صحيح.");

            validator.RuleFor(x => x.FirstName)
                .MaximumLength(100).WithMessage("الاسم الأول طويل جداً.");

            validator.RuleFor(x => x.LastName)
                .MaximumLength(100).WithMessage("اسم العائلة طويل جداً.");
        }
    }

    public class CreateAddressCommandFluentValidator : AbstractValidator<Commands.Admin.CreateAddressCommand>
    {
        public CreateAddressCommandFluentValidator()
        {
            AddressRules.Apply(this);
        }
    }

    public class UpdateAddressCommandFluentValidator : AbstractValidator<Commands.Admin.UpdateAddressCommand>
    {
        public UpdateAddressCommandFluentValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("معرّف العنوان مطلوب.");
            AddressRules.Apply(this);
        }
    }
}
