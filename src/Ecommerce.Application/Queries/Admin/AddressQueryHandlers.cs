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
    public class GetMyAddressesQueryHandler : IQueryHandler<GetMyAddressesQuery, List<AddressDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public GetMyAddressesQueryHandler(IApplicationDbContext db, IMapper mapper, ICurrentUserService currentUser)
        {
            _db = db;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<List<AddressDto>> Handle(GetMyAddressesQuery query, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId ?? throw new DomainException("User is not authenticated");

            var addresses = await _db.Addresses
                .AsNoTracking()
                .Where(a => a.UserId == userId && !a.IsDeleted)
                .OrderBy(a => a.CreatedAt)
                .ToListAsync(cancellationToken);

            return _mapper.Map<List<AddressDto>>(addresses);
        }
    }

    public class GetAddressByIdQueryHandler : IQueryHandler<GetAddressByIdQuery, AddressDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public GetAddressByIdQueryHandler(IApplicationDbContext db, IMapper mapper, ICurrentUserService currentUser)
        {
            _db = db;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<AddressDto> Handle(GetAddressByIdQuery query, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId ?? throw new DomainException("User is not authenticated");

            var address = await _db.Addresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == query.Id && a.UserId == userId && !a.IsDeleted, cancellationToken);
            if (address == null)
                throw new NotFoundException("Address", query.Id);

            return _mapper.Map<AddressDto>(address);
        }
    }
}
