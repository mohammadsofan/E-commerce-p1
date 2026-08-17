using System;
using System.Collections.Generic;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminVendorsQuery : IQuery<PagedResult<VendorDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }

    public class GetAdminVendorByIdQuery : IQuery<VendorDto>
    {
        public Guid Id { get; set; }
    }

    public class GetVendorProductsQuery : IQuery<List<VendorProductDto>>
    {
        public Guid VendorId { get; set; }
    }
}