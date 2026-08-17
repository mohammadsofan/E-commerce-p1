using System;

namespace Ecommerce.Domain.Entities
{
    public class ProductAttribute
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
}
