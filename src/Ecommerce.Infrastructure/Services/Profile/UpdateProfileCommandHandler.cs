using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Infrastructure.Services.Profile
{
    public class UpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand, AdminUserDto>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public UpdateProfileCommandHandler(UserManager<ApplicationUser> userManager, IMapper mapper, ICurrentUserService currentUser)
        {
            _userManager = userManager;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<AdminUserDto> Handle(UpdateProfileCommand command, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId ?? throw new DomainException("User is not authenticated");
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                throw new DomainException("User not found");

            user.FirstName = command.FirstName;
            user.LastName = command.LastName;
            user.DisplayName = command.DisplayName;
            user.Gender = command.Gender;
            user.DateOfBirth = command.DateOfBirth;
            user.ProfileImageUrl = command.ProfileImageUrl;
            user.PhoneNumber = command.PhoneNumber;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new DomainException($"Failed to update profile: {errors}");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var dto = _mapper.Map<AdminUserDto>(user);
            dto.Roles = roles.ToList();
            return dto;
        }
    }
}