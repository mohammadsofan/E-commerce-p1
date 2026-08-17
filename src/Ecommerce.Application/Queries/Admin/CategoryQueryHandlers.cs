using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetCategoriesQueryHandler : IQueryHandler<GetCategoriesQuery, List<CategoryDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetCategoriesQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<CategoryDto>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken = default)
        {
            var categories = await _db.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);

            return _mapper.Map<List<CategoryDto>>(categories);
        }
    }

    public class GetCategoryBySlugQueryHandler : IQueryHandler<GetCategoryBySlugQuery, CategoryDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetCategoryBySlugQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<CategoryDto> Handle(GetCategoryBySlugQuery query, CancellationToken cancellationToken = default)
        {
            var category = await _db.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == query.Slug, cancellationToken);
            if (category == null || !category.IsActive || category.IsDeleted)
                throw new DomainException("Category not found");

            return _mapper.Map<CategoryDto>(category);
        }
    }
}
