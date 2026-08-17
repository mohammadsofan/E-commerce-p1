using System;
using System.Collections.Generic;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetProductReviewsQuery : IQuery<List<ProductReviewDto>>
    {
        public Guid ProductId { get; set; }
    }

    public class GetAdminReviewsQuery : IQuery<PagedResult<ProductReviewDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? ProductId { get; set; }
        public bool? IsApproved { get; set; }
        public int? MinRating { get; set; }
    }
}