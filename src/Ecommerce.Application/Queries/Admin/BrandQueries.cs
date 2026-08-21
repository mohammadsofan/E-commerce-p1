using System.Collections.Generic;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetBrandsQuery : IQuery<List<BrandDto>>
    {
    }

    public class GetBrandByIdQuery : IQuery<BrandDto>
    {
        public System.Guid Id { get; set; }
    }
}
