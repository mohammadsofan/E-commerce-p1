using System;
using System.Collections.Generic;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Products
{
    public class GetProductsQuery : IQuery<PagedResult<ProductDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? BrandId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? IsActive { get; set; }

        /// <summary>
        /// Admin-only escape hatch. The public catalog defaults to published products only.
        /// </summary>
        public bool IncludeInactive { get; set; }

        public string? SortBy { get; set; } // name, price_asc, price_desc, newest, featured, highest_rated
        public string? Tag { get; set; }    // filter by tag name stored in SeoKeywords

        /// <summary>Restrict results to featured products (independent of the sort order).</summary>
        public bool? IsFeatured { get; set; }
    }
}