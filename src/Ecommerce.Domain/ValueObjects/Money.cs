using System;

namespace Ecommerce.Domain.ValueObjects
{
    public sealed class Money
    {
        public decimal Amount { get; }
        public string CurrencyCode { get; }

        public Money(decimal amount, string currencyCode)
        {
            if (amount < 0) throw new ArgumentException("Amount cannot be negative", nameof(amount));
            Amount = amount;
            CurrencyCode = currencyCode ?? throw new ArgumentNullException(nameof(currencyCode));
        }

        public override string ToString() => $"{CurrencyCode} {Amount:N2}";
    }
}
