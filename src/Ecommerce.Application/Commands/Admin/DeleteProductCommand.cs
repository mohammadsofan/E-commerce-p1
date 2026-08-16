using System;

namespace Ecommerce.Application.Commands.Admin
{
    public class DeleteProductCommand
    {
        public Guid Id { get; set; }
        public bool HardDelete { get; set; } = false;
    }
}