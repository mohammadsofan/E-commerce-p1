using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetSalesReportQuery : IQuery<SalesReportDto>
    {
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string GroupBy { get; set; } = "day";
    }

    public class GetRevenueReportQuery : IQuery<RevenueReportDto>
    {
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string GroupBy { get; set; } = "day";
    }

    public class GetInventoryReportQuery : IQuery<InventoryReportDto>
    {
        public DateTimeOffset? AsOfDate { get; set; }
        public List<Guid> WarehouseIds { get; set; } = new();
        public List<Guid> CategoryIds { get; set; } = new();
    }

    public class GetCustomerReportQuery : IQuery<CustomerReportDto>
    {
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
    }

    public class ExportReportQuery : IQuery<ExportResult>
    {
        public string ReportType { get; set; } = string.Empty; // sales, revenue, inventory, customer
        public ReportParameters Parameters { get; set; } = new();
    }
}