using System;
using System.Threading.Tasks;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly QueryDispatcher _queryDispatcher;

        public AdminDashboardController(QueryDispatcher queryDispatcher)
        {
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets admin dashboard metrics and statistics</summary>
        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var query = new GetAdminDashboardQuery();
            var result = await _queryDispatcher.Send<GetAdminDashboardQuery, AdminDashboardDto>(query);
            return Ok(result);
        }
    }
}