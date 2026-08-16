using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminPaymentsQuery : IQuery<PagedResult<AdminPaymentDto>>
    {
        public Guid? OrderId { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetAdminPaymentByIdQuery : IQuery<AdminPaymentDto>
    {
        public Guid Id { get; set; }
    }

    public class GetAdminRefundsQuery : IQuery<PagedResult<AdminRefundDto>>
    {
        public Guid? PaymentId { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetAdminRefundByIdQuery : IQuery<AdminRefundDto>
    {
        public Guid Id { get; set; }
    }
}