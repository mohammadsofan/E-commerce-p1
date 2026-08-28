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
    public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, AdminUserDto>
    {
        private readonly IUserManagementService _userManagement;
        private readonly IMapper _mapper;

        public CreateUserCommandHandler(IUserManagementService userManagement, IMapper mapper)
        {
            _userManagement = userManagement;
            _mapper = mapper;
        }

        public async Task<AdminUserDto> Handle(CreateUserCommand command, CancellationToken cancellationToken = default)
        {
            return await _userManagement.CreateUserAsync(
                command.Email,
                command.EffectiveUserName,
                command.Password,
                command.FirstName,
                command.LastName,
                command.DisplayName,
                command.PhoneNumber,
                command.Roles,
                cancellationToken);
        }
    }
}