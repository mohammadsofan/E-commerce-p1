using System;
using System.Collections.Generic;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.StoreFeatures
{
    public class GetActiveFeaturesQuery : IQuery<List<StoreFeatureDto>>
    {
    }

    public class GetAdminFeaturesQuery : IQuery<PagedResult<StoreFeatureDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }

    public class GetFeatureByIdQuery : IQuery<StoreFeatureDto>
    {
        public Guid Id { get; set; }
    }
}
