using System.Collections.Generic;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetTagsQuery : IQuery<List<TagDto>>
    {
    }

    public class GetAdminTagsQuery : IQuery<PagedResult<TagDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
    }
}