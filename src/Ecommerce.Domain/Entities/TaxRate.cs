using System;

namespace Ecommerce.Domain.Entities
{
    public class TaxRate
    {
        public Guid Id { get; set; }
        public Guid TaxCategoryId { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string RegionCode { get; set; } = string.Empty;
        public string? PostalCodePattern { get; set; }
        public decimal Rate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public TaxCategory TaxCategory { get; set; } = null!;
    }
}
