using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Infrastructure.Services.Profile
{
    public class GetMyProfileQueryHandler : IQueryHandler<GetMyProfileQuery, AdminUserDto>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public GetMyProfileQueryHandler(UserManager<ApplicationUser> userManager, IMapper mapper, ICurrentUserService currentUser)
        {
            _userManager = userManager;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<AdminUserDto> Handle(GetMyProfileQuery query, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId ?? throw new DomainException("User is not authenticated");
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                throw new DomainException("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            var dto = _mapper.Map<AdminUserDto>(user);
            dto.Roles = roles.ToList();
            return dto;
        }
    }
}