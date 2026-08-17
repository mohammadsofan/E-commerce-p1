using System;
using System.Collections.Generic;

namespace Ecommerce.Application.DTOs
{
    public class AdminShippingZoneDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public List<AdminShippingZoneLocationDto> Locations { get; set; } = new();
        public List<AdminShippingMethodDto> Methods { get; set; } = new();
    }

    public class AdminShippingZoneLocationDto
    {
        public Guid Id { get; set; }
        public Guid ShippingZoneId { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string? RegionCode { get; set; }
        public string? PostalCodePattern { get; set; }
    }

    public class AdminShippingMethodDto
    {
        public Guid Id { get; set; }
        public Guid ShippingZoneId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal BaseRate { get; set; }
        public decimal? FreeShippingThreshold { get; set; }
        public int? EstimatedDaysMin { get; set; }
        public int? EstimatedDaysMax { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public List<AdminShippingRateDto> Rates { get; set; } = new();
    }

    public class AdminShippingRateDto
    {
        public Guid Id { get; set; }
        public Guid ShippingMethodId { get; set; }
        public string ConditionType { get; set; } = string.Empty;
        public string ConditionOperator { get; set; } = string.Empty;
        public decimal ConditionValueMin { get; set; }
        public decimal ConditionValueMax { get; set; }
        public decimal Rate { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}