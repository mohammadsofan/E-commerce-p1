using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminUsersQueryHandler : IQueryHandler<GetAdminUsersQuery, PagedResult<AdminUserDto>>
    {
        private readonly IUserManagementService _userManagement;
        private readonly IMapper _mapper;

        public GetAdminUsersQueryHandler(IUserManagementService userManagement, IMapper mapper)
        {
            _userManagement = userManagement;
            _mapper = mapper;
        }

        public async Task<PagedResult<AdminUserDto>> Handle(GetAdminUsersQuery query, CancellationToken cancellationToken = default)
        {
            return await _userManagement.GetUsersAsync(
                query.Page,
                query.PageSize,
                query.Search,
                query.Role,
                query.IsActive,
                query.IncludeDeleted,
                cancellationToken);
        }
    }
}