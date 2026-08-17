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
    public class GetMySupportTicketsQueryHandler : IQueryHandler<GetMySupportTicketsQuery, List<SupportTicketDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public GetMySupportTicketsQueryHandler(IApplicationDbContext db, IMapper mapper, ICurrentUserService currentUser)
        {
            _db = db;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<List<SupportTicketDto>> Handle(GetMySupportTicketsQuery query, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId ?? throw new DomainException("User is not authenticated");

            var tickets = await _db.SupportTickets
                .Include(t => t.Messages)
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);

            return _mapper.Map<List<SupportTicketDto>>(tickets);
        }
    }

    public class GetSupportTicketByIdQueryHandler : IQueryHandler<GetSupportTicketByIdQuery, SupportTicketDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public GetSupportTicketByIdQueryHandler(IApplicationDbContext db, IMapper mapper, ICurrentUserService currentUser)
        {
            _db = db;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<SupportTicketDto> Handle(GetSupportTicketByIdQuery query, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId ?? throw new DomainException("User is not authenticated");

            var ticket = await _db.SupportTickets
                .Include(t => t.Messages)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == query.Id && t.UserId == userId, cancellationToken);
            if (ticket == null)
                throw new DomainException("Support ticket not found");

            return _mapper.Map<SupportTicketDto>(ticket);
        }
    }

    public class GetAdminSupportTicketsQueryHandler : IQueryHandler<GetAdminSupportTicketsQuery, PagedResult<SupportTicketDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminSupportTicketsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<SupportTicketDto>> Handle(GetAdminSupportTicketsQuery query, CancellationToken cancellationToken = default)
        {
            var page = System.Math.Max(1, query.Page);
            var pageSize = System.Math.Clamp(query.PageSize, 1, 100);

            var q = _db.SupportTickets
                .Include(t => t.Messages)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Status))
                q = q.Where(t => t.Status == query.Status);

            if (!string.IsNullOrWhiteSpace(query.Priority))
                q = q.Where(t => t.Priority == query.Priority);

            if (query.AssignedToUserId.HasValue)
                q = q.Where(t => t.AssignedToUserId == query.AssignedToUserId.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                q = q.Where(t => t.Subject.Contains(query.Search) ||
                                t.UserId.ToString().Contains(query.Search));
            }

            var totalCount = await q.CountAsync(cancellationToken);
            var items = await q
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<SupportTicketDto>
            {
                Items = _mapper.Map<List<SupportTicketDto>>(items),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }

    public class GetAdminSupportTicketByIdQueryHandler : IQueryHandler<GetAdminSupportTicketByIdQuery, SupportTicketDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminSupportTicketByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<SupportTicketDto> Handle(GetAdminSupportTicketByIdQuery query, CancellationToken cancellationToken = default)
        {
            var ticket = await _db.SupportTickets
                .Include(t => t.Messages)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken);
            if (ticket == null)
                throw new DomainException("Support ticket not found");

            return _mapper.Map<SupportTicketDto>(ticket);
        }
    }
}