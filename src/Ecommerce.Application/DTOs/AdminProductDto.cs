using System;
using System.Collections.Generic;

namespace Ecommerce.Application.DTOs
{
    public class AdminProductDto
    {
        public Guid Id { get; set; }
        public Guid? BrandId { get; set; }
        public Guid? CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal CompareAtPrice { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public decimal Weight { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsDigital { get; set; }
        public bool RequiresShipping { get; set; }
        public bool TrackInventory { get; set; }
        public bool AllowBackorder { get; set; }
        public string SeoTitle { get; set; } = string.Empty;
        public string SeoDescription { get; set; } = string.Empty;
        public string SeoKeywords { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public int Stock { get; set; }
        public int AvailableStock { get; set; }
        public Guid? WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public List<AdminProductVariantDto> Variants { get; set; } = new List<AdminProductVariantDto>();
        public List<AdminProductImageDto> Images { get; set; } = new List<AdminProductImageDto>();
    }

    public class AdminProductVariantDto
    {
        public Guid Id { get; set; }
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
        public bool IsActive { get; set; }
        public bool TrackInventory { get; set; }
        public bool AllowBackorder { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public List<AdminProductImageDto> Images { get; set; } = new();
        public List<AdminProductVariantAttributeDto> Attributes { get; set; } = new();
    }

    public class AdminProductImageDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string AltText { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class AdminProductAttributeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string DisplayType { get; set; } = string.Empty;
        public bool IsFilterable { get; set; }
        public bool IsVariant { get; set; }
        public bool IsRequired { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class AdminProductVariantAttributeDto
    {
        public Guid Id { get; set; }
        public Guid ProductVariantId { get; set; }
        public Guid ProductAttributeId { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public string AttributeCode { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}