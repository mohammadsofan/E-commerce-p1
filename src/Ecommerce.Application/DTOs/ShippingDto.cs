using System;
using System.Collections.Generic;

namespace Ecommerce.Application.DTOs
{
    public class ShippingZoneDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<ShippingMethodDto> Methods { get; set; } = new();
    }

    public class ShippingMethodDto
    {
        public Guid Id { get; set; }
        public Guid ShippingZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal BaseRate { get; set; }
        public decimal? FreeShippingThreshold { get; set; }
        public int? EstimatedDaysMin { get; set; }
        public int? EstimatedDaysMax { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
    }
}
