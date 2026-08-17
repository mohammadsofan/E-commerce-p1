using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminShippingZonesQuery : IQuery<PagedResult<AdminShippingZoneDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
    }

    public class GetAdminShippingZoneByIdQuery : IQuery<AdminShippingZoneDto>
    {
        public Guid Id { get; set; }
    }

    public class GetAdminShippingMethodsQuery : IQuery<PagedResult<AdminShippingMethodDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? ShippingZoneId { get; set; }
        public bool? IsActive { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class GetAdminShippingMethodByIdQuery : IQuery<AdminShippingMethodDto>
    {
        public Guid Id { get; set; }
    }

    public class GetAdminShippingRatesQuery : IQuery<PagedResult<AdminShippingRateDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? ShippingMethodId { get; set; }
    }

    public class GetAdminShippingRateByIdQuery : IQuery<AdminShippingRateDto>
    {
        public Guid Id { get; set; }
    }
}