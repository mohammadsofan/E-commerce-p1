using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetBrandsQueryHandler : IQueryHandler<GetBrandsQuery, List<BrandDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetBrandsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<BrandDto>> Handle(GetBrandsQuery query, CancellationToken cancellationToken = default)
        {
            var brandQuery = _db.Brands
                .AsNoTracking()
                .Where(b => b.IsActive && !b.IsDeleted);

            if (query.CategoryId.HasValue)
            {
                var brandIdsInCategory = await _db.Products
                    .AsNoTracking()
                    .Where(p => p.CategoryId == query.CategoryId.Value && !p.IsDeleted && p.IsActive && p.BrandId.HasValue)
                    .Select(p => p.BrandId!.Value)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                brandQuery = brandQuery.Where(b => brandIdsInCategory.Contains(b.Id));
            }

            var brands = await brandQuery
                .OrderBy(b => b.Name)
                .ToListAsync(cancellationToken);

            return _mapper.Map<List<BrandDto>>(brands);
        }
    }

    public class GetBrandByIdQueryHandler : IQueryHandler<GetBrandByIdQuery, BrandDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetBrandByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<BrandDto> Handle(GetBrandByIdQuery query, CancellationToken cancellationToken = default)
        {
            var brand = await _db.Brands
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == query.Id, cancellationToken);
            if (brand == null || brand.IsDeleted)
                throw new Ecommerce.Domain.Exceptions.NotFoundException("Brand", query.Id);

            return _mapper.Map<BrandDto>(brand);
        }
    }
}
