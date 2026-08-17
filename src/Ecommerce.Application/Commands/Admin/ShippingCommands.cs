using System;
using System.Collections.Generic;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateShippingZoneCommand : ICommand<AdminShippingZoneDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public List<CreateShippingZoneLocationCommand> Locations { get; set; } = new();
    }

    public class CreateShippingZoneLocationCommand
    {
        public string CountryCode { get; set; } = string.Empty;
        public string? RegionCode { get; set; }
        public string? PostalCodePattern { get; set; }
    }

    public class UpdateShippingZoneCommand : ICommand<AdminShippingZoneDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public List<UpdateShippingZoneLocationCommand> Locations { get; set; } = new();
    }

    public class UpdateShippingZoneLocationCommand
    {
        public Guid? Id { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string? RegionCode { get; set; }
        public string? PostalCodePattern { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class DeleteShippingZoneCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }

    public class CreateShippingMethodCommand : ICommand<AdminShippingMethodDto>
    {
        public Guid ShippingZoneId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal BaseRate { get; set; }
        public decimal? FreeShippingThreshold { get; set; }
        public int? EstimatedDaysMin { get; set; }
        public int? EstimatedDaysMax { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }
        public List<CreateShippingRateCommand> Rates { get; set; } = new();
    }

    public class CreateShippingRateCommand
    {
        public string ConditionType { get; set; } = string.Empty;
        public string ConditionOperator { get; set; } = string.Empty;
        public decimal ConditionValueMin { get; set; }
        public decimal ConditionValueMax { get; set; }
        public decimal Rate { get; set; }
    }

    public class UpdateShippingMethodCommand : ICommand<AdminShippingMethodDto>
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
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public List<UpdateShippingRateCommand> Rates { get; set; } = new();
    }

    public class UpdateShippingRateCommand
    {
        public Guid? Id { get; set; }
        public string ConditionType { get; set; } = string.Empty;
        public string ConditionOperator { get; set; } = string.Empty;
        public decimal ConditionValueMin { get; set; }
        public decimal ConditionValueMax { get; set; }
        public decimal Rate { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class DeleteShippingMethodCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }

    public class CreateShippingRateOnlyCommand : ICommand<AdminShippingRateDto>
    {
        public Guid ShippingMethodId { get; set; }
        public string ConditionType { get; set; } = string.Empty;
        public string ConditionOperator { get; set; } = string.Empty;
        public decimal ConditionValueMin { get; set; }
        public decimal ConditionValueMax { get; set; }
        public decimal Rate { get; set; }
    }

    public class UpdateShippingRateOnlyCommand : ICommand<AdminShippingRateDto>
    {
        public Guid Id { get; set; }
        public string ConditionType { get; set; } = string.Empty;
        public string ConditionOperator { get; set; } = string.Empty;
        public decimal ConditionValueMin { get; set; }
        public decimal ConditionValueMax { get; set; }
        public decimal Rate { get; set; }
    }

    public class DeleteShippingRateCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }
}