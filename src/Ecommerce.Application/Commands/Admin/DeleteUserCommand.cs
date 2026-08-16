using System;

namespace Ecommerce.Application.Commands.Admin
{
    public class DeleteUserCommand
    {
        public Guid Id { get; set; }
        public bool HardDelete { get; set; } = false;
    }
}