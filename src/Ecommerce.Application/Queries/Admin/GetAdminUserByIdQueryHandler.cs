using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Domain.Exceptions;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminUserByIdQueryHandler : IQueryHandler<GetAdminUserByIdQuery, AdminUserDto>
    {
        private readonly IUserManagementService _userManagement;
        private readonly IMapper _mapper;

        public GetAdminUserByIdQueryHandler(IUserManagementService userManagement, IMapper mapper)
        {
            _userManagement = userManagement;
            _mapper = mapper;
        }

        public async Task<AdminUserDto> Handle(GetAdminUserByIdQuery query, CancellationToken cancellationToken = default)
        {
            return await _userManagement.GetUserByIdAsync(query.Id, cancellationToken);
        }
    }
}