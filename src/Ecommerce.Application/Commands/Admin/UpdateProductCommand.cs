using System;

namespace Ecommerce.Application.Commands.Admin
{
    public class UpdateProductCommand
    {
        public Guid Id { get; set; }
        public Guid? BrandId { get; set; }
        public Guid? CategoryId { get; set; }
        public required string Name { get; set; }
        public required string Slug { get; set; }
        public required string Sku { get; set; }
        public string ShortDescription { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ProductType { get; set; } = "Simple";
        public string Status { get; set; } = "Draft";
        public decimal BasePrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal CompareAtPrice { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public Guid? TaxCategoryId { get; set; }
        public decimal Weight { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; }
        public bool IsDigital { get; set; }
        public bool RequiresShipping { get; set; } = true;
        public bool TrackInventory { get; set; } = true;
        public bool AllowBackorder { get; set; }
        public string SeoTitle { get; set; } = string.Empty;
        public string SeoDescription { get; set; } = string.Empty;
        public string SeoKeywords { get; set; } = string.Empty;
        public int? Stock { get; set; }
        public Guid? WarehouseId { get; set; }
    }
}