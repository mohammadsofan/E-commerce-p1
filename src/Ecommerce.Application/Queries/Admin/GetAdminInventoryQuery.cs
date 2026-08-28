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

        /// <summary>
        /// Backordered rows (negative availability) are the ones an operator most needs to see,
        /// so they are included by default. Pass false to hide them.
        /// </summary>
        public bool IncludeBackorder { get; set; } = true;
    }
}