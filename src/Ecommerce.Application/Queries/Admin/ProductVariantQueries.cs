using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminProductVariantsQuery : IQuery<PagedResult<AdminProductVariantDto>>
    {
        public Guid? ProductId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
    }

    public class GetAdminProductVariantByIdQuery : IQuery<AdminProductVariantDto>
    {
        public Guid Id { get; set; }
    }

    public class GetAdminProductImagesQuery : IQuery<PagedResult<AdminProductImageDto>>
    {
        public Guid? ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetAdminProductAttributesQuery : IQuery<PagedResult<AdminProductAttributeDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
        public bool? IsVariant { get; set; }
    }

    public class GetAdminProductAttributeByIdQuery : IQuery<AdminProductAttributeDto>
    {
        public Guid Id { get; set; }
    }
}