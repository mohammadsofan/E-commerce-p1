using System;

namespace Ecommerce.Application.Interfaces
{
    public interface IApplicationUser
    {
        Guid Id { get; }
        string Email { get; }
        string UserName { get; }
        string FirstName { get; }
        string LastName { get; }
        string DisplayName { get; }
        string Gender { get; }
        DateTimeOffset? DateOfBirth { get; }
        string PhoneNumber { get; }
        bool IsActive { get; }
        DateTimeOffset CreatedAt { get; }
        DateTimeOffset? LastLoginAt { get; }
        bool IsEmailVerified { get; }
        bool IsPhoneVerified { get; }
    }
}