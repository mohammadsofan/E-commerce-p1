using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminInventoryQuery : IQuery<PagedResult<AdminInventoryDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public Guid? ProductId { get; set; }
        public Guid? WarehouseId { get; set; }
        public bool? LowStockOnly { get; set; }
        public bool IncludeBackorder { get; set; } = false;
    }
}