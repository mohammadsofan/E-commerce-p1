using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;

namespace Ecommerce.Application.Commands.Admin
{
    public class SetUserRolesCommandHandler : ICommandHandler<SetUserRolesCommand, Unit>
    {
        private readonly IUserManagementService _userManagement;

        public SetUserRolesCommandHandler(IUserManagementService userManagement)
        {
            _userManagement = userManagement;
        }

        public async Task<Unit> Handle(SetUserRolesCommand command, CancellationToken cancellationToken = default)
        {
            await _userManagement.SetUserRolesAsync(command.UserId, command.Roles, cancellationToken);
            return new Unit();
        }
    }
}