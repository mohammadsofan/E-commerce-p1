using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminWarehousesQuery : IQuery<PagedResult<WarehouseDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }

    public class GetAdminWarehouseByIdQuery : IQuery<WarehouseDto>
    {
        public Guid Id { get; set; }
    }
}
