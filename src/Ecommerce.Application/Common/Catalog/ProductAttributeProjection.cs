using System;
using System.Collections.Generic;
using System.Linq;
using Ecommerce.Application.DTOs;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Common.Catalog
{
    /// <summary>
    /// Builds the customer-facing option matrix (e.g. Colour → [Black, Olive], Size → [M, L])
    /// for a product.
    /// <para>
    /// The matrix is derived from the variants' own <see cref="ProductVariantAttribute"/> rows,
    /// which makes it impossible for the advertised options to drift away from the variants that
    /// actually exist. <c>Product.AttributesJson</c> is only consulted as a fallback for products
    /// that expose options without having variants.
    /// </para>
    /// </summary>
    public static class ProductAttributeProjection
    {
        public static List<ProductAttributeOptionDto> Resolve(Product product)
        {
            var fromVariants = FromVariants(product);
            return fromVariants.Count > 0 ? fromVariants : FromJson(product.AttributesJson);
        }

        private static List<ProductAttributeOptionDto> FromVariants(Product product)
        {
            if (product.Variants == null || product.Variants.Count == 0)
                return new List<ProductAttributeOptionDto>();

            var rows = product.Variants
                .Where(v => v.IsActive)
                .SelectMany(v => v.VariantAttributes ?? (ICollection<ProductVariantAttribute>)new List<ProductVariantAttribute>())
                .Where(va => va.ProductAttribute != null && !string.IsNullOrWhiteSpace(va.Value))
                .ToList();

            if (rows.Count == 0)
                return new List<ProductAttributeOptionDto>();

            return rows
                .GroupBy(va => va.ProductAttributeId)
                .Select(g =>
                {
                    var attribute = g.First().ProductAttribute!;
                    return new ProductAttributeOptionDto
                    {
                        AttributeId = attribute.Id.ToString(),
                        Name = attribute.Name,
                        Code = attribute.Code,
                        DisplayType = string.IsNullOrWhiteSpace(attribute.DisplayType) ? "Select" : attribute.DisplayType,
                        Values = g
                            .Select(va => va.Value)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                            .ToList()
                    };
                })
                .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<ProductAttributeOptionDto> FromJson(string? attributesJson)
        {
            if (string.IsNullOrWhiteSpace(attributesJson))
                return new List<ProductAttributeOptionDto>();

            try
            {
                return System.Text.Json.JsonSerializer
                           .Deserialize<List<ProductAttributeOptionDto>>(attributesJson)
                       ?? new List<ProductAttributeOptionDto>();
            }
            catch
            {
                return new List<ProductAttributeOptionDto>();
            }
        }
    }
}
