using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminTaxCategoriesQuery : IQuery<PagedResult<AdminTaxCategoryDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
    }

    public class GetAdminTaxCategoryByIdQuery : IQuery<AdminTaxCategoryDto>
    {
        public Guid Id { get; set; }
    }

    public class GetAdminTaxRatesQuery : IQuery<PagedResult<AdminTaxRateDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? TaxCategoryId { get; set; }
        public string? CountryCode { get; set; }
        public bool? IsActive { get; set; }
    }

    public class GetAdminTaxRateByIdQuery : IQuery<AdminTaxRateDto>
    {
        public Guid Id { get; set; }
    }
}