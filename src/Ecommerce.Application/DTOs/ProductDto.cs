using System;
using System.Collections.Generic;

namespace Ecommerce.Application.DTOs
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public bool IsActive { get; set; }
        public CategoryDto? Category { get; set; }
        public BrandDto? Brand { get; set; }
        public List<AdminProductImageDto> Images { get; set; } = new List<AdminProductImageDto>();
        public List<string> Tags { get; set; } = new List<string>();
        public List<ProductAttributeOptionDto> Attributes { get; set; } = new List<ProductAttributeOptionDto>();
        public List<ProductVariantDto> Variants { get; set; } = new List<ProductVariantDto>();
        public int AvailableStock { get; set; }
        public decimal? PromotionalPrice { get; set; }
        public int? DiscountPercentage { get; set; }
        public string? PromotionName { get; set; }
        public string? PromotionBadge { get; set; }
        public bool IsOnSale => (PromotionalPrice.HasValue && PromotionalPrice.Value < BasePrice) || !string.IsNullOrWhiteSpace(PromotionBadge);
    }

    /// <summary>
    /// Lightweight variant projection returned to the customer storefront.
    /// Contains only the fields needed for attribute matching and pricing;
    /// no cost or administrative fields are exposed.
    /// </summary>
    public class ProductVariantDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public int AvailableStock { get; set; }
        public List<ProductVariantAttributeDto> Attributes { get; set; } = new List<ProductVariantAttributeDto>();
    }

    /// <summary>
    /// A single attribute–value pair on a variant (e.g. Color=Red).
    /// Used by the frontend to resolve the concrete variant from the
    /// user's attribute selections before submitting the cart payload.
    /// </summary>
    public class ProductVariantAttributeDto
    {
        public string AttributeName { get; set; } = string.Empty;
        public string AttributeCode { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class ProductAttributeOptionDto
    {
        public string AttributeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string DisplayType { get; set; } = string.Empty;
        public List<string> Values { get; set; } = new List<string>();
    }
}
