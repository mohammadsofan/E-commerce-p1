using System;
using System.Collections.Generic;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateProductVariantCommand : ICommand<AdminProductVariantDto>
    {
        public Guid ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public decimal CompareAtPrice { get; set; }
        public decimal Weight { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public bool IsActive { get; set; } = true;
        public bool TrackInventory { get; set; } = true;
        public bool AllowBackorder { get; set; } = false;
        public List<CreateProductImageCommand> Images { get; set; } = new();
        public List<CreateProductVariantAttributeCommand> Attributes { get; set; } = new();
    }

    public class CreateProductImageCommand
    {
        public string Url { get; set; } = string.Empty;
        public string AltText { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
    }

    public class CreateProductVariantAttributeCommand
    {
        public Guid ProductAttributeId { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}