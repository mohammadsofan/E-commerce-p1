using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Products
{
    public class GetProductByIdQuery : IQuery<ProductDto>
    {
        public System.Guid Id { get; set; }

        /// <summary>
        /// Admin-only: allows reading an unpublished or soft-deleted product.
        /// Storefront callers leave this false so hidden products stay hidden.
        /// </summary>
        public bool IncludeUnpublished { get; set; }
    }
}
