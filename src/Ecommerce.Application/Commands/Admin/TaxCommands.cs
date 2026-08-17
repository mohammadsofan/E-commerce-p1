using System;
using System.Collections.Generic;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateTaxCategoryCommand : ICommand<AdminTaxCategoryDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public List<CreateTaxRateCommand> Rates { get; set; } = new();
    }

    public class CreateTaxRateCommand
    {
        public string CountryCode { get; set; } = string.Empty;
        public string RegionCode { get; set; } = string.Empty;
        public string? PostalCodePattern { get; set; }
        public decimal Rate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateTaxCategoryCommand : ICommand<AdminTaxCategoryDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public List<UpdateTaxRateCommand> Rates { get; set; } = new();
    }

    public class UpdateTaxRateCommand
    {
        public Guid? Id { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string RegionCode { get; set; } = string.Empty;
        public string? PostalCodePattern { get; set; }
        public decimal Rate { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class DeleteTaxCategoryCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }

    public class CreateTaxRateOnlyCommand : ICommand<AdminTaxRateDto>
    {
        public Guid TaxCategoryId { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string RegionCode { get; set; } = string.Empty;
        public string? PostalCodePattern { get; set; }
        public decimal Rate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateTaxRateOnlyCommand : ICommand<AdminTaxRateDto>
    {
        public Guid Id { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string RegionCode { get; set; } = string.Empty;
        public string? PostalCodePattern { get; set; }
        public decimal Rate { get; set; }
        public bool IsActive { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public class DeleteTaxRateCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }
}