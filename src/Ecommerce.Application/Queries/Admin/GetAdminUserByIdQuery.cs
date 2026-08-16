using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminUserByIdQuery : IQuery<AdminUserDto>
    {
        public Guid Id { get; set; }
    }
}