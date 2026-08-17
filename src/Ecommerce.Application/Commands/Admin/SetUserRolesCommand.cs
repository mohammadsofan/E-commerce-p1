using System;
using System.Collections.Generic;

namespace Ecommerce.Application.Commands.Admin
{
    public class SetUserRolesCommand
    {
        public Guid UserId { get; set; }
        public required List<string> Roles { get; set; }
    }
}