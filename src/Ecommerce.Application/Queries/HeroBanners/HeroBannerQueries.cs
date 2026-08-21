using System;
using System.Collections.Generic;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.HeroBanners
{
    public class GetActiveHeroBannerQuery : IQuery<HeroBannerDto?>
    {
    }

    public class GetActiveHeroBannersQuery : IQuery<List<HeroBannerDto>>
    {
    }

    public class GetAdminHeroBannersQuery : IQuery<PagedResult<HeroBannerDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }

    public class GetHeroBannerByIdQuery : IQuery<HeroBannerDto>
    {
        public Guid Id { get; set; }
    }
}
