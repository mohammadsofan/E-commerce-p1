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
    public class GetMyProfileQueryHandler : IQueryHandler<GetMyProfileQuery, UserProfileDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public GetMyProfileQueryHandler(IApplicationDbContext db, IMapper mapper, ICurrentUserService currentUser)
        {
            _db = db;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<UserProfileDto> Handle(GetMyProfileQuery query, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId ?? throw new DomainException("User is not authenticated");

            var profile = await _db.UserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
            if (profile == null)
                return new UserProfileDto { UserId = userId };

            return _mapper.Map<UserProfileDto>(profile);
        }
    }
}