using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Products
{
    public class GetProductByIdQuery : IQuery<ProductDto>
    {
        public System.Guid Id { get; set; }
    }
}
