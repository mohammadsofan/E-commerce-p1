using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateUserCommand
    {
        public required string Email { get; set; }

        /// <summary>
        /// Optional: admin UIs generally only collect an email. When omitted the email is used,
        /// matching how self-registration creates accounts.
        /// </summary>
        public string? UserName { get; set; }

        public required string Password { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();

        [JsonIgnore]
        public string EffectiveUserName =>
            string.IsNullOrWhiteSpace(UserName) ? (Email ?? string.Empty).Trim() : UserName.Trim();
    }
}