using System;
using System.Collections.Generic;

namespace Ecommerce.Application.DTOs
{
    public class AdminProductDto
    {
        public Guid Id { get; set; }
        public Guid? BrandId { get; set; }
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
        public Guid? TaxCategoryId { get; set; }
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
        public List<AdminProductVariantDto> Variants { get; set; } = new List<AdminProductVariantDto>();
        public List<AdminProductImageDto> Images { get; set; } = new List<AdminProductImageDto>();
    }

    public class AdminProductVariantDto
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
    }

    public class AdminProductImageDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string AltText { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
    }
}