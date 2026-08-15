using System;

namespace Ecommerce.Domain.Entities
{
    public class ProductAttribute
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string DisplayType { get; set; }
        public bool IsFilterable { get; set; }
        public bool IsVariant { get; set; }
        public bool IsRequired { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
