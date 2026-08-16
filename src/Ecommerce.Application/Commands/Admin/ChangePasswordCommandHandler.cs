using System;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;

namespace Ecommerce.Application.Commands.Admin
{
    public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, Unit>
    {
        private readonly IUserManagementService _userManagement;

        public ChangePasswordCommandHandler(IUserManagementService userManagement)
        {
            _userManagement = userManagement;
        }

        public async Task<Unit> Handle(ChangePasswordCommand command, CancellationToken cancellationToken = default)
        {
            await _userManagement.ChangePasswordAsync(command.UserId, command.CurrentPassword, command.NewPassword, cancellationToken);
            return new Unit();
        }
    }
}