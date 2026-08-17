using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public UserManagementService(UserManager<ApplicationUser> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<AdminUserDto> CreateUserAsync(string email, string userName, string password, string firstName, string lastName, string displayName, string phoneNumber, List<string> roles, CancellationToken cancellationToken = default)
        {
            // Check if email is unique
            var existingEmail = await _userManager.FindByEmailAsync(email);
            if (existingEmail != null)
                throw new DomainException($"User with email '{email}' already exists.");

            // Check if username is unique
            var existingUserName = await _userManager.FindByNameAsync(userName);
            if (existingUserName != null)
                throw new DomainException($"User with username '{userName}' already exists.");

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                DisplayName = displayName,
                PhoneNumber = phoneNumber,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsEmailVerified = false,
                IsPhoneVerified = false
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new DomainException($"Failed to create user: {errors}");
            }

            // Assign roles
            if (roles.Count > 0)
            {
                var validRoles = new[] { "Admin", "Customer" };
                var rolesToAdd = roles.Where(r => validRoles.Contains(r)).ToList();
                if (rolesToAdd.Count > 0)
                {
                    await _userManager.AddToRolesAsync(user, rolesToAdd);
                }
            }

            var dto = _mapper.Map<AdminUserDto>(user);
            var rolesList = await _userManager.GetRolesAsync(user);
            dto.Roles = rolesList.ToList();
            return dto;
        }

        public async Task<AdminUserDto> UpdateUserAsync(Guid id, string email, string userName, string firstName, string lastName, string displayName, string phoneNumber, bool isActive, bool isEmailVerified, bool isPhoneVerified, List<string> roles, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                throw new NotFoundException("User", id);

            // Check if email is unique (excluding current user)
            if (!string.IsNullOrWhiteSpace(email))
            {
                var existingEmail = await _userManager.FindByEmailAsync(email);
                if (existingEmail != null && existingEmail.Id != id)
                    throw new DomainException($"User with email '{email}' already exists.");
            }

            // Check if username is unique (excluding current user)
            if (!string.IsNullOrWhiteSpace(userName))
            {
                var existingUserName = await _userManager.FindByNameAsync(userName);
                if (existingUserName != null && existingUserName.Id != id)
                    throw new DomainException($"User with username '{userName}' already exists.");
            }

            user.Email = email;
            user.UserName = userName;
            user.FirstName = firstName;
            user.LastName = lastName;
            user.DisplayName = displayName;
            user.PhoneNumber = phoneNumber;
            user.IsActive = isActive;
            user.IsEmailVerified = isEmailVerified;
            user.IsPhoneVerified = isPhoneVerified;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new DomainException($"Failed to update user: {errors}");
            }

            // Update roles
            if (roles.Count > 0)
            {
                var validRoles = new[] { "Admin", "Customer" };
                var rolesToAdd = roles.Where(r => validRoles.Contains(r)).ToList();
                
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                
                if (rolesToAdd.Count > 0)
                {
                    await _userManager.AddToRolesAsync(user, rolesToAdd);
                }
            }

            var dto = _mapper.Map<AdminUserDto>(user);
            var rolesList = await _userManager.GetRolesAsync(user);
            dto.Roles = rolesList.ToList();
            return dto;
        }

        public async Task DeleteUserAsync(Guid id, bool hardDelete, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                throw new NotFoundException("User", id);

            if (hardDelete)
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new DomainException($"Failed to delete user: {errors}");
                }
            }
            else
            {
                user.IsActive = false;
                user.UpdatedAt = DateTimeOffset.UtcNow;
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new DomainException($"Failed to deactivate user: {errors}");
                }
            }
        }

        public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                throw new NotFoundException("User", userId);

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new DomainException($"Failed to change password: {errors}");
            }
        }

        public async Task SetUserRolesAsync(Guid userId, List<string> roles, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                throw new NotFoundException("User", userId);

            var validRoles = new[] { "Admin", "Customer" };
            var rolesToAdd = roles.Where(r => validRoles.Contains(r)).ToList();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new DomainException($"Failed to remove roles: {errors}");
            }

            if (rolesToAdd.Count > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                    throw new DomainException($"Failed to add roles: {errors}");
                }
            }
        }

        public async Task<PagedResult<AdminUserDto>> GetUsersAsync(int page, int pageSize, string? search, string? role, bool? isActive, bool includeDeleted, CancellationToken cancellationToken = default)
        {
            var q = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(u => u.Email.Contains(search) ||
                                u.UserName.Contains(search) ||
                                u.FirstName.Contains(search) ||
                                u.LastName.Contains(search));
            }

            if (isActive.HasValue)
                q = q.Where(u => u.IsActive == isActive.Value);

            var totalCount = await q.CountAsync(cancellationToken);

            var users = await q
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var items = new List<AdminUserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var dto = _mapper.Map<AdminUserDto>(user);
                dto.Roles = roles.ToList();
                items.Add(dto);
            }

            return new PagedResult<AdminUserDto>
            {
                Items = items,
                TotalCount = items.Count,
                Page = 1,
                PageSize = pageSize
            };
        }

        public async Task<AdminUserDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                throw new NotFoundException("User", id);

            var roles = await _userManager.GetRolesAsync(user);
            var dto = _mapper.Map<AdminUserDto>(user);
            dto.Roles = roles.ToList();
            return dto;
        }
    }
}