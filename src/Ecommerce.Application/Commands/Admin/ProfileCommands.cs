using System;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class UpdateProfileCommand : ICommand<UserProfileDto>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTimeOffset? DateOfBirth { get; set; }
        public string ProfileImageUrl { get; set; } = string.Empty;
    }
}