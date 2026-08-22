using System;

namespace Ecommerce.Domain.Entities
{
    public class StoreSetting
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public decimal StandardShippingCost { get; set; } = 15m;
        public decimal? FreeShippingThreshold { get; set; } = 50m;
        public string StoreName { get; set; } = "Sofan Store";
        public string? ContactEmail { get; set; } = "mohammad.n.sofan@gmail.com";
        public string? ContactPhone { get; set; }
        public string CurrencyCode { get; set; } = "ILS";
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
