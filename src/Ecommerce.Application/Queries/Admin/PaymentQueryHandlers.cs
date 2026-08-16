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
    public class GetAdminPaymentsQueryHandler : IQueryHandler<GetAdminPaymentsQuery, PagedResult<AdminPaymentDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminPaymentsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminPaymentDto>> Handle(GetAdminPaymentsQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.Payments.AsQueryable();

            if (query.OrderId.HasValue)
                q = q.Where(p => p.OrderId == query.OrderId.Value);

            if (!string.IsNullOrWhiteSpace(query.Status))
                q = q.Where(p => p.Status == query.Status);

            var totalCount = await q.CountAsync(cancellationToken);

            var payments = await q
                .OrderByDescending(p => p.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AdminPaymentDto>
            {
                Items = _mapper.Map<List<AdminPaymentDto>>(payments),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetAdminPaymentByIdQueryHandler : IQueryHandler<GetAdminPaymentByIdQuery, AdminPaymentDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminPaymentByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminPaymentDto> Handle(GetAdminPaymentByIdQuery query, CancellationToken cancellationToken = default)
        {
            var payment = await _db.Payments.FindAsync(new object[] { query.Id }, cancellationToken);

            if (payment == null)
                throw new Domain.Exceptions.NotFoundException("Payment", query.Id);

            return _mapper.Map<AdminPaymentDto>(payment);
        }
    }

    public class GetAdminRefundsQueryHandler : IQueryHandler<GetAdminRefundsQuery, PagedResult<AdminRefundDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminRefundsQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminRefundDto>> Handle(GetAdminRefundsQuery query, CancellationToken cancellationToken = default)
        {
            var q = _db.Refunds.AsQueryable();

            if (query.PaymentId.HasValue)
                q = q.Where(r => r.PaymentId == query.PaymentId.Value);

            if (!string.IsNullOrWhiteSpace(query.Status))
                q = q.Where(r => r.Status == query.Status);

            var totalCount = await q.CountAsync(cancellationToken);

            var refunds = await q
                .OrderByDescending(r => r.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AdminRefundDto>
            {
                Items = _mapper.Map<List<AdminRefundDto>>(refunds),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }

    public class GetAdminRefundByIdQueryHandler : IQueryHandler<GetAdminRefundByIdQuery, AdminRefundDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminRefundByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminRefundDto> Handle(GetAdminRefundByIdQuery query, CancellationToken cancellationToken = default)
        {
            var refund = await _db.Refunds.FindAsync(new object[] { query.Id }, cancellationToken);

            if (refund == null)
                throw new Domain.Exceptions.NotFoundException("Refund", query.Id);

            return _mapper.Map<AdminRefundDto>(refund);
        }
    }
}