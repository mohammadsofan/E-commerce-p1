using System;
using Microsoft.AspNetCore.Identity;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser<Guid>, IApplicationUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTimeOffset? DateOfBirth { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsPhoneVerified { get; set; }

        // IApplicationUser implementation - explicitly map to the interface
        string IApplicationUser.FirstName => FirstName;
        string IApplicationUser.LastName => LastName;
        string IApplicationUser.DisplayName => DisplayName;
        string IApplicationUser.Gender => Gender;
        DateTimeOffset? IApplicationUser.DateOfBirth => DateOfBirth;
        string IApplicationUser.PhoneNumber => PhoneNumber;
        bool IApplicationUser.IsActive => IsActive;
        DateTimeOffset IApplicationUser.CreatedAt => CreatedAt;
        DateTimeOffset? IApplicationUser.LastLoginAt => LastLoginAt;
        bool IApplicationUser.IsEmailVerified => IsEmailVerified;
        bool IApplicationUser.IsPhoneVerified => IsPhoneVerified;
    }
}
