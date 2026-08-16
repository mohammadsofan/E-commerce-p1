using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Products
{
    public class GetProductBySlugQuery : IQuery<ProductDto>
    {
        public string Slug { get; set; } = string.Empty;
    }
}
