using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminProductByIdQuery : IQuery<AdminProductDto>
    {
        public Guid Id { get; set; }
    }
}