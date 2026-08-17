using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminAuditLogsQueryHandler : IQueryHandler<GetAdminAuditLogsQuery, PagedResult<AuditLogDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminAuditLogsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AuditLogDto>> Handle(GetAdminAuditLogsQuery query, CancellationToken cancellationToken = default)
        {
            var page = System.Math.Max(1, query.Page);
            var pageSize = System.Math.Clamp(query.PageSize, 1, 100);

            var q = _db.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.EntityName))
                q = q.Where(l => l.EntityName == query.EntityName);

            if (!string.IsNullOrWhiteSpace(query.Action))
                q = q.Where(l => l.Action == query.Action);

            if (query.UserId.HasValue)
                q = q.Where(l => l.UserId == query.UserId.Value);

            if (query.FromDate.HasValue)
                q = q.Where(l => l.CreatedAt >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                q = q.Where(l => l.CreatedAt <= query.ToDate.Value);

            var totalCount = await q.CountAsync(cancellationToken);
            var items = await q
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AuditLogDto>
            {
                Items = _mapper.Map<List<AuditLogDto>>(items),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }

    public class GetAdminAuditLogByIdQueryHandler : IQueryHandler<GetAdminAuditLogByIdQuery, AuditLogDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminAuditLogByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AuditLogDto> Handle(GetAdminAuditLogByIdQuery query, CancellationToken cancellationToken = default)
        {
            var log = await _db.AuditLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == query.Id, cancellationToken);
            if (log == null)
                throw new DomainException("Audit log not found");

            return _mapper.Map<AuditLogDto>(log);
        }
    }
}