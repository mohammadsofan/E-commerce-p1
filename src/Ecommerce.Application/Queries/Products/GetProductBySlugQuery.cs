using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Products
{
    public class GetProductBySlugQuery : IQuery<ProductDto>
    {
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Admin-only: allows reading an unpublished or soft-deleted product.
        /// </summary>
        public bool IncludeUnpublished { get; set; }
    }
}
