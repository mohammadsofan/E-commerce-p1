using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateVendorCommand : ICommand<VendorDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class UpdateVendorCommand : ICommand<VendorDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class DeleteVendorCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }

    public class CreateVendorProductCommand : ICommand<VendorProductDto>
    {
        public Guid VendorId { get; set; }
        public Guid ProductId { get; set; }
        public string VendorSku { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class DeleteVendorProductCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }
}