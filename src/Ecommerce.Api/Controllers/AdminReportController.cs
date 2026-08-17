using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/admin/reports")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminReportController : ControllerBase
    {
        [HttpGet("sales")]
        public async Task<IActionResult> GetSalesReport(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string groupBy = "day")
        {
            return Ok(new { message = "Sales report endpoint - to be implemented" });
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueReport(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string groupBy = "day")
        {
            return Ok(new { message = "Revenue report endpoint - to be implemented" });
        }

        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventoryReport(
            [FromQuery] DateTime? asOfDate = null,
            [FromQuery] List<Guid>? warehouseIds = null,
            [FromQuery] List<Guid>? categoryIds = null)
        {
            return Ok(new { message = "Inventory report endpoint - to be implemented" });
        }

        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomerReport(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            return Ok(new { message = "Customer report endpoint - to be implemented" });
        }

        [HttpPost("export")]
        public async Task<IActionResult> ExportReport([FromBody] ExportReportRequest request)
        {
            return Ok(new { message = "Export report endpoint - to be implemented" });
        }
    }

    public class ExportReportRequest
    {
        public string ReportType { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string GroupBy { get; set; } = "day";
        public List<Guid> CategoryIds { get; set; } = new();
        public List<Guid> WarehouseIds { get; set; } = new();
        public string Format { get; set; } = "csv";
    }
}