using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminProductsQuery : IQuery<PagedResult<AdminProductDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public string? Status { get; set; }
        public Guid? BrandId { get; set; }
        public bool? IsActive { get; set; }
        public bool IncludeDeleted { get; set; } = false;
    }
}