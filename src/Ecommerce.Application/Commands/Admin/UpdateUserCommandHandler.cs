using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;

namespace Ecommerce.Application.Commands.Admin
{
    public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, AdminUserDto>
    {
        private readonly IUserManagementService _userManagement;
        private readonly IMapper _mapper;

        public UpdateUserCommandHandler(IUserManagementService userManagement, IMapper mapper)
        {
            _userManagement = userManagement;
            _mapper = mapper;
        }

        public async Task<AdminUserDto> Handle(UpdateUserCommand command, CancellationToken cancellationToken = default)
        {
            return await _userManagement.UpdateUserAsync(
                command.Id,
                command.Email,
                command.UserName,
                command.FirstName,
                command.LastName,
                command.DisplayName,
                command.PhoneNumber,
                command.IsActive,
                command.IsEmailVerified,
                command.IsPhoneVerified,
                command.Roles,
                cancellationToken);
        }
    }
}