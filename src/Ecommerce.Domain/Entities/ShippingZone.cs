using System;
using System.Collections.Generic;

namespace Ecommerce.Domain.Entities
{
    public class ShippingZone
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<ShippingZoneLocation> Locations { get; set; } = new List<ShippingZoneLocation>();
        public ICollection<ShippingMethod> Methods { get; set; } = new List<ShippingMethod>();
    }

    public class ShippingZoneLocation
    {
        public Guid Id { get; set; }
        public Guid ShippingZoneId { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string? RegionCode { get; set; }
        public string? PostalCodePattern { get; set; }

        public ShippingZone ShippingZone { get; set; } = null!;
    }

    public class ShippingMethod
    {
        public Guid Id { get; set; }
        public Guid ShippingZoneId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // flat_rate, weight_based, price_based, free
        public decimal BaseRate { get; set; }
        public decimal? FreeShippingThreshold { get; set; }
        public int? EstimatedDaysMin { get; set; }
        public int? EstimatedDaysMax { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ShippingZone ShippingZone { get; set; } = null!;
        public ICollection<ShippingRate> Rates { get; set; } = new List<ShippingRate>();
    }

    public class ShippingRate
    {
        public Guid Id { get; set; }
        public Guid ShippingMethodId { get; set; }
        public string ConditionType { get; set; } = string.Empty; // weight, price, quantity
        public string ConditionOperator { get; set; } = string.Empty; // >=, <=, between
        public decimal ConditionValueMin { get; set; }
        public decimal ConditionValueMax { get; set; }
        public decimal Rate { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public ShippingMethod ShippingMethod { get; set; } = null!;
    }
}