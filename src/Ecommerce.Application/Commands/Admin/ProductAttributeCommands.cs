using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateProductAttributeCommand : ICommand<AdminProductAttributeDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string DisplayType { get; set; } = "text";
        public bool IsFilterable { get; set; }
        public bool IsVariant { get; set; }
        public bool IsRequired { get; set; }
    }

    public class UpdateProductAttributeCommand : ICommand<AdminProductAttributeDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string DisplayType { get; set; } = "text";
        public bool IsFilterable { get; set; }
        public bool IsVariant { get; set; }
        public bool IsRequired { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public class DeleteProductAttributeCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }
}