using System;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;

namespace Ecommerce.Application.Commands.Admin
{
    public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, Unit>
    {
        private readonly IUserManagementService _userManagement;

        public DeleteUserCommandHandler(IUserManagementService userManagement)
        {
            _userManagement = userManagement;
        }

        public async Task<Unit> Handle(DeleteUserCommand command, CancellationToken cancellationToken = default)
        {
            await _userManagement.DeleteUserAsync(command.Id, command.HardDelete, cancellationToken);
            return new Unit();
        }
    }
}