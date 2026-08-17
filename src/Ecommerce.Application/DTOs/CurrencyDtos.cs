using System;
using System.Collections.Generic;

namespace Ecommerce.Application.DTOs
{
    public class CurrencyDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public bool IsBaseCurrency { get; set; }
    }

    public class ExchangeRateDto
    {
        public Guid Id { get; set; }
        public Guid FromCurrencyId { get; set; }
        public string FromCurrencyCode { get; set; } = string.Empty;
        public Guid ToCurrencyId { get; set; }
        public string ToCurrencyCode { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public DateTimeOffset EffectiveAt { get; set; }
    }

    public class CurrencyConversionResult
    {
        public decimal Amount { get; set; }
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public decimal ConvertedAmount { get; set; }
        public DateTimeOffset AsOf { get; set; }
    }
}