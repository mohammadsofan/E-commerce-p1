using System;
using System.Collections.Generic;

namespace Ecommerce.Application.DTOs
{
    public class AdminTaxCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public List<AdminTaxRateDto> Rates { get; set; } = new();
    }

    public class AdminTaxRateDto
    {
        public Guid Id { get; set; }
        public Guid TaxCategoryId { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string RegionCode { get; set; } = string.Empty;
        public string? PostalCodePattern { get; set; }
        public decimal Rate { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}