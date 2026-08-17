using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/admin/tax")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminTaxController : ControllerBase
    {
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            return Ok(new { message = "Tax categories endpoint - to be implemented" });
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory()
        {
            return Ok(new { message = "Create tax category endpoint - to be implemented" });
        }

        [HttpPut("categories/{id:guid}")]
        public async Task<IActionResult> UpdateCategory(Guid id)
        {
            return Ok(new { message = "Update tax category endpoint - to be implemented" });
        }

        [HttpDelete("categories/{id:guid}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            return Ok(new { message = "Delete tax category endpoint - to be implemented" });
        }

        [HttpGet("rates")]
        public async Task<IActionResult> GetRates()
        {
            return Ok(new { message = "Tax rates endpoint - to be implemented" });
        }

        [HttpPost("rates")]
        public async Task<IActionResult> CreateRate()
        {
            return Ok(new { message = "Create tax rate endpoint - to be implemented" });
        }

        [HttpPut("rates/{id:guid}")]
        public async Task<IActionResult> UpdateRate(Guid id)
        {
            return Ok(new { message = "Update tax rate endpoint - to be implemented" });
        }

        [HttpDelete("rates/{id:guid}")]
        public async Task<IActionResult> DeleteRate(Guid id)
        {
            return Ok(new { message = "Delete tax rate endpoint - to be implemented" });
        }
    }
}