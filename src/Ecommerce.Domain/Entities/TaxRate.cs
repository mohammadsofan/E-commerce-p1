using System;

namespace Ecommerce.Domain.Entities
{
    public class TaxRate
    {
        public Guid Id { get; set; }
        public Guid TaxCategoryId { get; set; }
        public string CountryCode { get; set; }
        public string RegionCode { get; set; }
        public decimal Rate { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
