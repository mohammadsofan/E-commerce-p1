using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Interfaces
{
    public interface IUserManagementService
    {
        Task<AdminUserDto> CreateUserAsync(string email, string userName, string password, string firstName, string lastName, string displayName, string phoneNumber, List<string> roles, CancellationToken cancellationToken = default);
        Task<AdminUserDto> UpdateUserAsync(Guid id, string email, string userName, string firstName, string lastName, string displayName, string phoneNumber, bool isActive, bool isEmailVerified, bool isPhoneVerified, List<string> roles, CancellationToken cancellationToken = default);
        Task DeleteUserAsync(Guid id, bool hardDelete, CancellationToken cancellationToken = default);
        Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
        Task SetUserRolesAsync(Guid userId, List<string> roles, CancellationToken cancellationToken = default);
        Task<PagedResult<AdminUserDto>> GetUsersAsync(int page, int pageSize, string? search, string? role, bool? isActive, bool includeDeleted, CancellationToken cancellationToken = default);
        Task<AdminUserDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    }
}