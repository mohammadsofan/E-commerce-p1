using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminOrderByIdQuery : IQuery<OrderDto>
    {
        public Guid Id { get; set; }
    }
}