using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class UpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand, UserProfileDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public UpdateProfileCommandHandler(IApplicationDbContext db, IMapper mapper, ICurrentUserService currentUser)
        {
            _db = db;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<UserProfileDto> Handle(UpdateProfileCommand command, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId ?? throw new DomainException("User is not authenticated");
            var now = DateTimeOffset.UtcNow;

            var profile = await _db.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (profile == null)
            {
                profile = new UserProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _db.UserProfiles.Add(profile);
            }

            profile.FirstName = command.FirstName;
            profile.LastName = command.LastName;
            profile.DisplayName = command.DisplayName;
            profile.Gender = command.Gender;
            profile.DateOfBirth = command.DateOfBirth;
            profile.ProfileImageUrl = command.ProfileImageUrl;
            profile.UpdatedAt = now;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<UserProfileDto>(profile);
        }
    }
}