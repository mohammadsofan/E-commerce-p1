using System.Collections.Generic;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetCategoriesQuery : IQuery<List<CategoryDto>>
    {
    }

    public class GetCategoryBySlugQuery : IQuery<CategoryDto>
    {
        public string Slug { get; set; }
    }

    public class GetCategoryByIdQuery : IQuery<CategoryDto>
    {
        public System.Guid Id { get; set; }
    }
}
