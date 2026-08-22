using System;

namespace Ecommerce.Application.DTOs
{
    public class StoreSettingsDto
    {
        public Guid Id { get; set; }
        public decimal StandardShippingCost { get; set; }
        public decimal? FreeShippingThreshold { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string CurrencyCode { get; set; } = "ILS";
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class ShippingSettingsDto
    {
        public decimal StandardShippingCost { get; set; }
        public decimal? FreeShippingThreshold { get; set; }
        public string CurrencyCode { get; set; } = "ILS";
    }
}
