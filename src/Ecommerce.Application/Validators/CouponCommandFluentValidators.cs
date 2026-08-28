using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;

namespace Ecommerce.Application.Validators
{
    /// <summary>
    /// Guards the discount engine's inputs. Without these a 500% coupon or a negative value
    /// could be saved and then applied to a live cart.
    /// </summary>
    internal static class CouponRules
    {
        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "percentage", "fixed_amount", "free_shipping"
        };

        public static void Apply<T>(AbstractValidator<T> validator, Func<T, string> type, Func<T, decimal> value)
        {
            validator.RuleFor(x => type(x))
                .Must(t => !string.IsNullOrWhiteSpace(t) && AllowedTypes.Contains(t.Trim()))
                .WithMessage($"نوع الكوبون غير مدعوم. الأنواع المتاحة: {string.Join(", ", AllowedTypes)}.");

            validator.RuleFor(x => value(x))
                .GreaterThanOrEqualTo(0m).WithMessage("قيمة الخصم لا يمكن أن تكون سالبة.");

            validator.RuleFor(x => value(x))
                .GreaterThan(0m)
                .When(x => !string.Equals(type(x)?.Trim(), "free_shipping", StringComparison.OrdinalIgnoreCase))
                .WithMessage("قيمة الخصم يجب أن تكون أكبر من صفر.");

            validator.RuleFor(x => value(x))
                .LessThanOrEqualTo(100m)
                .When(x => string.Equals(type(x)?.Trim(), "percentage", StringComparison.OrdinalIgnoreCase))
                .WithMessage("نسبة الخصم لا يمكن أن تتجاوز 100%.");
        }
    }

    public class CreateCouponCommandFluentValidator : AbstractValidator<Commands.Admin.CreateCouponCommand>
    {
        public CreateCouponCommandFluentValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("كود الكوبون مطلوب.")
                .MaximumLength(50).WithMessage("كود الكوبون طويل جداً.");

            CouponRules.Apply(this, x => x.Type, x => x.Value);

            RuleFor(x => x.MinOrderAmount)
                .GreaterThanOrEqualTo(0m).When(x => x.MinOrderAmount.HasValue)
                .WithMessage("الحد الأدنى للطلب لا يمكن أن يكون سالباً.");

            RuleFor(x => x.MaxDiscountAmount)
                .GreaterThan(0m).When(x => x.MaxDiscountAmount.HasValue)
                .WithMessage("الحد الأقصى للخصم يجب أن يكون أكبر من صفر.");

            RuleFor(x => x.UsageLimit)
                .GreaterThan(0).When(x => x.UsageLimit.HasValue)
                .WithMessage("حد الاستخدام يجب أن يكون أكبر من صفر.");

            RuleFor(x => x.PerUserLimit)
                .GreaterThan(0).When(x => x.PerUserLimit.HasValue)
                .WithMessage("حد الاستخدام لكل مستخدم يجب أن يكون أكبر من صفر.");

            RuleFor(x => x)
                .Must(x => !x.StartAt.HasValue || !x.EndAt.HasValue || x.EndAt.Value > x.StartAt.Value)
                .WithMessage("تاريخ انتهاء الكوبون يجب أن يكون بعد تاريخ البداية.");
        }
    }

    public class UpdateCouponCommandFluentValidator : AbstractValidator<Commands.Admin.UpdateCouponCommand>
    {
        public UpdateCouponCommandFluentValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("معرّف الكوبون مطلوب.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("كود الكوبون مطلوب.")
                .MaximumLength(50).WithMessage("كود الكوبون طويل جداً.");

            CouponRules.Apply(this, x => x.Type, x => x.Value);

            RuleFor(x => x.MinOrderAmount)
                .GreaterThanOrEqualTo(0m).When(x => x.MinOrderAmount.HasValue)
                .WithMessage("الحد الأدنى للطلب لا يمكن أن يكون سالباً.");

            RuleFor(x => x.MaxDiscountAmount)
                .GreaterThan(0m).When(x => x.MaxDiscountAmount.HasValue)
                .WithMessage("الحد الأقصى للخصم يجب أن يكون أكبر من صفر.");

            RuleFor(x => x.UsageLimit)
                .GreaterThan(0).When(x => x.UsageLimit.HasValue)
                .WithMessage("حد الاستخدام يجب أن يكون أكبر من صفر.");

            RuleFor(x => x.PerUserLimit)
                .GreaterThan(0).When(x => x.PerUserLimit.HasValue)
                .WithMessage("حد الاستخدام لكل مستخدم يجب أن يكون أكبر من صفر.");

            RuleFor(x => x)
                .Must(x => !x.StartAt.HasValue || !x.EndAt.HasValue || x.EndAt.Value > x.StartAt.Value)
                .WithMessage("تاريخ انتهاء الكوبون يجب أن يكون بعد تاريخ البداية.");
        }
    }
}
