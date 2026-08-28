using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentValidation;

namespace Ecommerce.Application.Validators
{
    /// <summary>
    /// Guards the promotion engine's inputs (D-07).
    ///
    /// <c>RulesJson</c> is free text on the wire, so before these rules a 250% percentage
    /// promotion could be saved and then applied to a live cart, producing negative line totals
    /// and giving products away. Everything the promotion evaluator can read out of a rule set —
    /// percentages, fixed amounts, buy/get quantities and tier definitions — is range-checked
    /// here, and malformed JSON is rejected outright rather than being silently ignored at
    /// evaluation time.
    /// </summary>
    internal static class PromotionRules
    {
        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "percentage",
            "percentage_discount",
            "fixed_amount",
            "fixed_discount",
            "buy_x_get_y",
            "bundle",
            "tiered_discount",
            "free_gift"
        };

        /// <summary>Keys the evaluator reads as a percentage. Must be within 0..100.</summary>
        private static readonly string[] PercentageKeys =
        {
            "percentage", "discountPercentage", "discount_percentage"
        };

        /// <summary>Keys the evaluator reads as a money amount. Must not be negative.</summary>
        private static readonly string[] AmountKeys =
        {
            "amount", "discountAmount", "bundlePrice", "minSpend", "min_spend"
        };

        /// <summary>Keys the evaluator reads as a count. Must be a positive integer.</summary>
        private static readonly string[] QuantityKeys =
        {
            "buyQuantity", "buy_quantity", "buy", "getQuantity", "get_quantity", "get"
        };

        public static void Apply<T>(
            AbstractValidator<T> validator,
            Func<T, string> name,
            Func<T, string> type,
            Func<T, string> rulesJson,
            Func<T, DateTimeOffset?> startAt,
            Func<T, DateTimeOffset?> endAt,
            Func<T, int?> usageLimit)
        {
            validator.RuleFor(x => name(x))
                .NotEmpty().WithMessage("اسم العرض مطلوب.")
                .MaximumLength(200).WithMessage("اسم العرض طويل جداً.");

            validator.RuleFor(x => type(x))
                .Must(t => !string.IsNullOrWhiteSpace(t) && AllowedTypes.Contains(t.Trim()))
                .WithMessage($"نوع العرض غير مدعوم. الأنواع المتاحة: {string.Join(", ", AllowedTypes)}.");

            validator.RuleFor(x => rulesJson(x))
                .Must(json => Validate(json, out _))
                .WithMessage(x => Validate(rulesJson(x), out var error) ? string.Empty : error);

            validator.RuleFor(x => usageLimit(x))
                .GreaterThan(0).When(x => usageLimit(x).HasValue)
                .WithMessage("حد الاستخدام يجب أن يكون أكبر من صفر.");

            validator.RuleFor(x => x)
                .Must(x => !startAt(x).HasValue || !endAt(x).HasValue || endAt(x)!.Value > startAt(x)!.Value)
                .WithMessage("تاريخ انتهاء العرض يجب أن يكون بعد تاريخ البداية.");
        }

        /// <summary>
        /// Validates a promotion rule set. An empty rule set is allowed (it simply yields no
        /// discount), but anything present must parse as a JSON object and every recognised
        /// numeric field must be in range.
        /// </summary>
        internal static bool Validate(string? rulesJson, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(rulesJson)) return true;

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(rulesJson);
            }
            catch (JsonException)
            {
                error = "قواعد العرض غير صالحة: يجب أن تكون بصيغة JSON صحيحة.";
                return false;
            }

            using (document)
            {
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    error = "قواعد العرض يجب أن تكون كائن JSON.";
                    return false;
                }

                return ValidateObject(document.RootElement, out error);
            }
        }

        private static bool ValidateObject(JsonElement element, out string error)
        {
            error = string.Empty;

            foreach (var property in element.EnumerateObject())
            {
                // "tiers" is an array of tier objects; each is validated with the same rules.
                if (property.NameEquals("tiers"))
                {
                    if (property.Value.ValueKind != JsonValueKind.Array)
                    {
                        error = "قواعد العرض غير صالحة: يجب أن تكون الشرائح (tiers) مصفوفة.";
                        return false;
                    }

                    foreach (var tier in property.Value.EnumerateArray())
                    {
                        if (tier.ValueKind != JsonValueKind.Object)
                        {
                            error = "قواعد العرض غير صالحة: كل شريحة يجب أن تكون كائن JSON.";
                            return false;
                        }

                        if (!ValidateTier(tier, out error)) return false;
                    }

                    continue;
                }

                if (!ValidateNumericProperty(property, out error)) return false;
            }

            return true;
        }

        /// <summary>
        /// A tier's <c>discount</c> means a percentage unless the tier (or the enclosing rule
        /// set) declares a fixed type, which mirrors how the evaluator reads it.
        /// </summary>
        private static bool ValidateTier(JsonElement tier, out string error)
        {
            error = string.Empty;

            var isFixed = false;
            foreach (var typeKey in new[] { "discountType", "discount_type", "type" })
            {
                if (tier.TryGetProperty(typeKey, out var typeValue) && typeValue.ValueKind == JsonValueKind.String)
                {
                    var declared = typeValue.GetString()?.Trim().ToLowerInvariant();
                    isFixed = declared is "fixed_amount" or "fixed" or "fixed_discount" or "amount";
                    break;
                }
            }

            foreach (var property in tier.EnumerateObject())
            {
                if (property.NameEquals("discount") || property.NameEquals("value"))
                {
                    if (property.Value.ValueKind != JsonValueKind.Number)
                    {
                        error = $"قواعد العرض غير صالحة: '{property.Name}' يجب أن يكون رقماً.";
                        return false;
                    }

                    var value = property.Value.GetDecimal();
                    if (value < 0m)
                    {
                        error = $"قواعد العرض غير صالحة: '{property.Name}' لا يمكن أن يكون سالباً.";
                        return false;
                    }

                    if (!isFixed && value > 100m)
                    {
                        error = "نسبة الخصم في العرض لا يمكن أن تتجاوز 100%.";
                        return false;
                    }

                    continue;
                }

                if (!ValidateNumericProperty(property, out error)) return false;
            }

            return true;
        }

        private static bool ValidateNumericProperty(JsonProperty property, out string error)
        {
            error = string.Empty;

            if (PercentageKeys.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (property.Value.ValueKind != JsonValueKind.Number)
                {
                    error = $"قواعد العرض غير صالحة: '{property.Name}' يجب أن يكون رقماً.";
                    return false;
                }

                var percentage = property.Value.GetDecimal();
                if (percentage < 0m)
                {
                    error = "نسبة الخصم في العرض لا يمكن أن تكون سالبة.";
                    return false;
                }

                if (percentage > 100m)
                {
                    error = "نسبة الخصم في العرض لا يمكن أن تتجاوز 100%.";
                    return false;
                }

                return true;
            }

            if (AmountKeys.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (property.Value.ValueKind != JsonValueKind.Number)
                {
                    error = $"قواعد العرض غير صالحة: '{property.Name}' يجب أن يكون رقماً.";
                    return false;
                }

                if (property.Value.GetDecimal() < 0m)
                {
                    error = $"قواعد العرض غير صالحة: '{property.Name}' لا يمكن أن يكون سالباً.";
                    return false;
                }

                return true;
            }

            if (QuantityKeys.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetInt32(out var quantity))
                {
                    error = $"قواعد العرض غير صالحة: '{property.Name}' يجب أن يكون عدداً صحيحاً.";
                    return false;
                }

                if (quantity <= 0)
                {
                    error = $"قواعد العرض غير صالحة: '{property.Name}' يجب أن يكون أكبر من صفر.";
                    return false;
                }
            }

            return true;
        }
    }

    public class CreatePromotionCommandFluentValidator : AbstractValidator<Commands.Admin.CreatePromotionCommand>
    {
        public CreatePromotionCommandFluentValidator()
        {
            PromotionRules.Apply(
                this,
                x => x.Name,
                x => x.Type,
                x => x.RulesJson,
                x => x.StartAt,
                x => x.EndAt,
                x => x.UsageLimit);
        }
    }

    public class UpdatePromotionCommandFluentValidator : AbstractValidator<Commands.Admin.UpdatePromotionCommand>
    {
        public UpdatePromotionCommandFluentValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("معرّف العرض مطلوب.");

            PromotionRules.Apply(
                this,
                x => x.Name,
                x => x.Type,
                x => x.RulesJson,
                x => x.StartAt,
                x => x.EndAt,
                x => x.UsageLimit);
        }
    }
}
