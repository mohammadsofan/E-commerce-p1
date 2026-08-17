using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateTagCommand : ICommand<TagDto>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateTagCommand : ICommand<TagDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class DeleteTagCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }
}