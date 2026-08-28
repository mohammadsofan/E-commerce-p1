using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/admin/reports")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminReportController : ControllerBase
    {
        private readonly QueryDispatcher _queryDispatcher;

        public AdminReportController(QueryDispatcher queryDispatcher)
        {
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet("sales")]
        public async Task<IActionResult> GetSalesReport(
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null,
            [FromQuery] string groupBy = "day")
        {
            var query = new GetSalesReportQuery
            {
                StartDate = startDate,
                EndDate = endDate,
                GroupBy = groupBy
            };
            var result = await _queryDispatcher.Send<GetSalesReportQuery, SalesReportDto>(query);
            return Ok(result);
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueReport(
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null,
            [FromQuery] string groupBy = "day")
        {
            var query = new GetRevenueReportQuery
            {
                StartDate = startDate,
                EndDate = endDate,
                GroupBy = groupBy
            };
            var result = await _queryDispatcher.Send<GetRevenueReportQuery, RevenueReportDto>(query);
            return Ok(result);
        }

        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventoryReport(
            [FromQuery] DateTimeOffset? asOfDate = null,
            [FromQuery] List<Guid>? warehouseIds = null,
            [FromQuery] List<Guid>? categoryIds = null)
        {
            var query = new GetInventoryReportQuery
            {
                AsOfDate = asOfDate,
                WarehouseIds = warehouseIds ?? new List<Guid>(),
                CategoryIds = categoryIds ?? new List<Guid>()
            };
            var result = await _queryDispatcher.Send<GetInventoryReportQuery, InventoryReportDto>(query);
            return Ok(result);
        }

        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomerReport(
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null)
        {
            var query = new GetCustomerReportQuery
            {
                StartDate = startDate,
                EndDate = endDate
            };
            var result = await _queryDispatcher.Send<GetCustomerReportQuery, CustomerReportDto>(query);
            return Ok(result);
        }

        [HttpPost("export")]
        public async Task<IActionResult> ExportReport([FromBody] ExportReportRequest request)
        {
            if (!string.Equals(request.Format, "csv", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.Format, "json", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Export format must be csv or json.");
            }

            var query = new ExportReportQuery
            {
                ReportType = request.ReportType,
                Parameters = new ReportParameters
                {
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    GroupBy = request.GroupBy,
                    CategoryIds = request.CategoryIds,
                    WarehouseIds = request.WarehouseIds,
                    Format = request.Format
                }
            };
            var result = await _queryDispatcher.Send<ExportReportQuery, ExportResult>(query);
            return File(result.Content, result.ContentType, result.FileName);
        }
    }

    public class ExportReportRequest
    {
        public string ReportType { get; set; } = string.Empty;
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string GroupBy { get; set; } = "day";
        public List<Guid> CategoryIds { get; set; } = new();
        public List<Guid> WarehouseIds { get; set; } = new();
        public string Format { get; set; } = "csv";
    }
}
