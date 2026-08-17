using System;

namespace Ecommerce.Application.Commands.Admin
{
    public class ChangePasswordCommand
    {
        public Guid UserId { get; set; }
        public required string CurrentPassword { get; set; }
        public required string NewPassword { get; set; }
    }
}