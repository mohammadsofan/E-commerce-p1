using System;

namespace Ecommerce.Application.DTOs
{
    public class AdminDashboardDto
    {
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal PendingOrdersRevenue { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public List<SalesByPeriodDto> SalesByPeriod { get; set; } = new List<SalesByPeriodDto>();
        public List<TopProductDto> TopProducts { get; set; } = new List<TopProductDto>();
        public List<TopCustomerDto> TopCustomers { get; set; } = new List<TopCustomerDto>();
    }

    public class SalesByPeriodDto
    {
        public string Period { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public int NewCustomers { get; set; }
    }

    public class TopProductDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopCustomerDto
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
    }
}