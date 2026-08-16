using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminInventoryByIdQuery : IQuery<AdminInventoryDto>
    {
        public Guid Id { get; set; }
    }
}