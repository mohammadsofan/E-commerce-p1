using System;

namespace Ecommerce.Domain.Entities
{
    public class ProductImage
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string Url { get; set; }
        public string AltText { get; set; }
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
