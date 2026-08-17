using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Orders
{
    public class GetOrderByIdQuery : IQuery<OrderDto>
    {
        public System.Guid Id { get; set; }
    }
}
