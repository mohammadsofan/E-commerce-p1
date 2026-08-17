using System;
using System.Collections.Generic;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class UpdateProductVariantCommand : ICommand<AdminProductVariantDto>
    {
        public Guid Id { get; set; }
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
        public bool IsActive { get; set; }
        public bool TrackInventory { get; set; }
        public bool AllowBackorder { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public List<UpdateProductImageCommand> Images { get; set; } = new();
        public List<UpdateProductVariantAttributeCommand> Attributes { get; set; } = new();
    }

    public class UpdateProductImageCommand
    {
        public Guid? Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string AltText { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class UpdateProductVariantAttributeCommand
    {
        public Guid? Id { get; set; }
        public Guid ProductAttributeId { get; set; }
        public string Value { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }
}