using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminShipmentsQuery : IQuery<PagedResult<ShipmentDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? OrderId { get; set; }
        public string? Status { get; set; }
    }

    public class GetAdminShipmentByIdQuery : IQuery<ShipmentDto>
    {
        public Guid Id { get; set; }
    }

    public class GetOrderShipmentQuery : IQuery<ShipmentDto>
    {
        public Guid OrderId { get; set; }
    }
}