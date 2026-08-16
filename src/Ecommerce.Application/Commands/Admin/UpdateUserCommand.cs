using System;
using System.Collections.Generic;

namespace Ecommerce.Application.Commands.Admin
{
    public class UpdateUserCommand
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsEmailVerified { get; set; }
        public bool IsPhoneVerified { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}