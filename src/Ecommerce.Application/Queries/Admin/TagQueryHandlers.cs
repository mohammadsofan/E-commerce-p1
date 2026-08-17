using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetTagsQueryHandler : IQueryHandler<GetTagsQuery, List<TagDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetTagsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<TagDto>> Handle(GetTagsQuery query, CancellationToken cancellationToken = default)
        {
            var tags = await _db.Tags
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);

            return _mapper.Map<List<TagDto>>(tags);
        }
    }

    public class GetAdminTagsQueryHandler : IQueryHandler<GetAdminTagsQuery, PagedResult<TagDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminTagsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<TagDto>> Handle(GetAdminTagsQuery query, CancellationToken cancellationToken = default)
        {
            var page = System.Math.Max(1, query.Page);
            var pageSize = System.Math.Clamp(query.PageSize, 1, 100);

            var q = _db.Tags.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.ToLower();
                q = q.Where(t => t.Name.ToLower().Contains(term) || t.Slug.ToLower().Contains(term));
            }

            var total = await q.CountAsync(cancellationToken);
            var items = await q
                .OrderBy(t => t.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<TagDto>
            {
                Items = _mapper.Map<List<TagDto>>(items),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}