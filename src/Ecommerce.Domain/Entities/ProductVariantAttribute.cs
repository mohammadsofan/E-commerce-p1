using System;

namespace Ecommerce.Domain.Entities
{
    public class ProductVariantAttribute
    {
        public Guid Id { get; set; }
        public Guid ProductVariantId { get; set; }
        public Guid ProductAttributeId { get; set; }
        public string Value { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public ProductAttribute ProductAttribute { get; set; } = null!;
    }
}