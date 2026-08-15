using System;
using System.Collections.Generic;

namespace Ecommerce.Domain.Entities
{
    public class Product
    {
        public Guid Id { get; set; }
        public Guid? BrandId { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Sku { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public string ProductType { get; set; }
        public string Status { get; set; }
        public decimal BasePrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal CompareAtPrice { get; set; }
        public string CurrencyCode { get; set; }
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
        public string SeoTitle { get; set; }
        public string SeoDescription { get; set; }
        public string SeoKeywords { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public byte[] RowVersion { get; set; }

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    }
}
