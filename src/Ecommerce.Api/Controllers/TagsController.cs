using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/tags")]
    public class TagsController : ControllerBase
    {
        private readonly QueryDispatcher _queryDispatcher;

        public TagsController(QueryDispatcher queryDispatcher)
        {
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var query = new GetTagsQuery();
            var result = await _queryDispatcher.Send<GetTagsQuery, List<TagDto>>(query);
            return Ok(result);
        }
    }
}