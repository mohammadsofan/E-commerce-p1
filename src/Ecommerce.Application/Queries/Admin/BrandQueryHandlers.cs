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
            var brands = await _db.Brands
                .AsNoTracking()
                .Where(b => b.IsActive && !b.IsDeleted)
                .OrderBy(b => b.Name)
                .ToListAsync(cancellationToken);

            return _mapper.Map<List<BrandDto>>(brands);
        }
    }
}
