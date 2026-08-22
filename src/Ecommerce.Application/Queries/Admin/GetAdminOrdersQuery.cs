using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminOrdersQuery : IQuery<PagedResult<OrderDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public OrderStatus? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public string? FulfillmentStatus { get; set; }
        public Guid? UserId { get; set; }
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
    }
}