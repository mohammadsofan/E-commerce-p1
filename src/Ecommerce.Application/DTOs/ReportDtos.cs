using System;

namespace Ecommerce.Application.DTOs
{
    public class SalesReportDto
    {
        public DateTimeOffset PeriodStart { get; set; }
        public DateTimeOffset PeriodEnd { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int NewCustomers { get; set; }
        public int ReturningCustomers { get; set; }
        public decimal ConversionRate { get; set; }
        public List<SalesByPeriodDto> SalesByPeriod { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
        public List<TopCategoryDto> TopCategories { get; set; } = new();
    }

    public class TopCategoryDto
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class RevenueReportDto
    {
        public DateTimeOffset PeriodStart { get; set; }
        public DateTimeOffset PeriodEnd { get; set; }
        public decimal GrossRevenue { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal TotalShipping { get; set; }
        public List<RevenueByPeriodDto> RevenueByPeriod { get; set; } = new();
        public List<RevenueByChannelDto> RevenueByChannel { get; set; } = new();
    }

    public class RevenueByPeriodDto
    {
        public string Period { get; set; } = string.Empty;
        public decimal GrossRevenue { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal Discounts { get; set; }
        public decimal Refunds { get; set; }
    }

    public class RevenueByChannelDto
    {
        public string Channel { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class InventoryReportDto
    {
        public DateTimeOffset AsOfDate { get; set; }
        public int TotalProducts { get; set; }
        public int TotalVariants { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public List<InventoryByWarehouseDto> ByWarehouse { get; set; } = new();
        public List<InventoryByCategoryDto> ByCategory { get; set; } = new();
    }

    public class InventoryByWarehouseDto
    {
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int VariantCount { get; set; }
        public decimal TotalValue { get; set; }
        public int LowStockCount { get; set; }
    }

    public class InventoryByCategoryDto
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int VariantCount { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class CustomerReportDto
    {
        public DateTimeOffset PeriodStart { get; set; }
        public DateTimeOffset PeriodEnd { get; set; }
        public int TotalCustomers { get; set; }
        public int NewCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public decimal AverageLifetimeValue { get; set; }
        public double RepeatPurchaseRate { get; set; }
        public List<CustomerSegmentDto> Segments { get; set; } = new();
    }

    public class CustomerSegmentDto
    {
        public string SegmentName { get; set; } = string.Empty;
        public int CustomerCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public class ReportParameters
    {
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string GroupBy { get; set; } = "day"; // day, week, month
        public List<Guid> CategoryIds { get; set; } = new();
        public List<Guid> WarehouseIds { get; set; } = new();
        public string Format { get; set; } = "json"; // json, csv, pdf
    }

    public class ExportResult
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}