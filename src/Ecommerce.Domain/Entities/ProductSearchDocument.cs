using System;

namespace Ecommerce.Domain.Entities
{
    /// <summary>
    /// Denormalized, searchable representation of a product used to serve
    /// fast text search without scanning the full product table. This can be
    /// backed by a dedicated table, a materialized view, or an external index
    /// (e.g., Elasticsearch) behind IProductSearchService.
    /// </summary>
    public class ProductSearchDocument
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string SearchText { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}