using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminProductVariantsQueryHandler : IQueryHandler<GetAdminProductVariantsQuery, PagedResult<AdminProductVariantDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminProductVariantsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminProductVariantDto>> Handle(GetAdminProductVariantsQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.ProductVariants
                .Include(v => v.Images)
                .Include(v => v.VariantAttributes)
                    .ThenInclude(va => va.ProductAttribute)
                .AsQueryable();

            if (query.ProductId.HasValue)
                q = q.Where(v => v.ProductId == query.ProductId.Value);

            if (query.IsActive.HasValue)
                q = q.Where(v => v.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.ToLower();
                q = q.Where(v => v.Name.ToLower().Contains(term) || v.Sku.ToLower().Contains(term));
            }

            var totalCount = await q.CountAsync(cancellationToken);

            var variants = await q
                .OrderBy(v => v.Name)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = variants.Select(v =>
            {
                var dto = _mapper.Map<AdminProductVariantDto>(v);
                dto.Images = v.Images.Select(_mapper.Map<AdminProductImageDto>).ToList();
                dto.Attributes = v.VariantAttributes.Select(va => new AdminProductVariantAttributeDto
                {
                    Id = va.Id,
                    ProductVariantId = va.ProductVariantId,
                    ProductAttributeId = va.ProductAttributeId,
                    AttributeName = va.ProductAttribute.Name,
                    AttributeCode = va.ProductAttribute.Code,
                    Value = va.Value,
                    CreatedAt = va.CreatedAt,
                    UpdatedAt = va.UpdatedAt
                }).ToList();
                return dto;
            }).ToList();

            return new PagedResult<AdminProductVariantDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetAdminProductVariantByIdQueryHandler : IQueryHandler<GetAdminProductVariantByIdQuery, AdminProductVariantDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminProductVariantByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminProductVariantDto> Handle(GetAdminProductVariantByIdQuery query, CancellationToken cancellationToken = default)
        {
            var variant = await _db.ProductVariants
                .Include(v => v.Images)
                .Include(v => v.VariantAttributes)
                    .ThenInclude(va => va.ProductAttribute)
                .FirstOrDefaultAsync(v => v.Id == query.Id, cancellationToken);

            if (variant == null)
                throw new Domain.Exceptions.NotFoundException("ProductVariant", query.Id);

            var dto = _mapper.Map<AdminProductVariantDto>(variant);
            dto.Images = variant.Images.Select(_mapper.Map<AdminProductImageDto>).ToList();
            dto.Attributes = variant.VariantAttributes.Select(va => new AdminProductVariantAttributeDto
            {
                Id = va.Id,
                ProductVariantId = va.ProductVariantId,
                ProductAttributeId = va.ProductAttributeId,
                AttributeName = va.ProductAttribute.Name,
                AttributeCode = va.ProductAttribute.Code,
                Value = va.Value,
                CreatedAt = va.CreatedAt,
                UpdatedAt = va.UpdatedAt
            }).ToList();

            return dto;
        }
    }

    public class GetAdminProductImagesQueryHandler : IQueryHandler<GetAdminProductImagesQuery, PagedResult<AdminProductImageDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminProductImagesQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminProductImageDto>> Handle(GetAdminProductImagesQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.ProductImages.AsQueryable();

            if (query.ProductId.HasValue)
                q = q.Where(i => i.ProductId == query.ProductId.Value);

            if (query.ProductVariantId.HasValue)
                q = q.Where(i => i.ProductVariantId == query.ProductVariantId.Value);

            var totalCount = await q.CountAsync(cancellationToken);

            var images = await q
                .OrderBy(i => i.SortOrder)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AdminProductImageDto>
            {
                Items = _mapper.Map<List<AdminProductImageDto>>(images),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetAdminProductAttributesQueryHandler : IQueryHandler<GetAdminProductAttributesQuery, PagedResult<AdminProductAttributeDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminProductAttributesQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminProductAttributeDto>> Handle(GetAdminProductAttributesQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.ProductAttributes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.ToLower();
                q = q.Where(a => a.Name.ToLower().Contains(term) || a.Code.ToLower().Contains(term));
            }

            if (query.IsVariant.HasValue)
                q = q.Where(a => a.IsVariant == query.IsVariant.Value);

            var totalCount = await q.CountAsync(cancellationToken);

            var attributes = await q
                .OrderBy(a => a.Name)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AdminProductAttributeDto>
            {
                Items = _mapper.Map<List<AdminProductAttributeDto>>(attributes),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetAdminProductAttributeByIdQueryHandler : IQueryHandler<GetAdminProductAttributeByIdQuery, AdminProductAttributeDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminProductAttributeByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminProductAttributeDto> Handle(GetAdminProductAttributeByIdQuery query, CancellationToken cancellationToken = default)
        {
            var attribute = await _db.ProductAttributes.FindAsync(new object[] { query.Id }, cancellationToken);

            if (attribute == null)
                throw new Domain.Exceptions.NotFoundException("ProductAttribute", query.Id);

            return _mapper.Map<AdminProductAttributeDto>(attribute);
        }
    }
}