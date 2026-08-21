using System.Collections.Generic;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Shipping
{
    public class GetActiveShippingMethodsQuery : IQuery<List<ShippingMethodDto>>
    {
    }

    public class GetActiveShippingZonesQuery : IQuery<List<ShippingZoneDto>>
    {
    }
}
