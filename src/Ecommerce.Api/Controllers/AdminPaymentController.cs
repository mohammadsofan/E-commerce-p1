using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/admin/payments")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminPaymentController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminPaymentController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? orderId = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = new GetAdminPaymentsQuery
            {
                OrderId = orderId,
                Status = status,
                Page = page,
                PageSize = pageSize
            };
            var result = await _queryDispatcher.Send<GetAdminPaymentsQuery, PagedResult<AdminPaymentDto>>(query);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAdminPaymentByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminPaymentByIdQuery, AdminPaymentDto>(query);
            return Ok(result);
        }

        [HttpPost("{id:guid}/capture")]
        public async Task<IActionResult> Capture(Guid id, [FromBody] CapturePaymentRequest request)
        {
            var command = new CapturePaymentCommand { PaymentId = id, Amount = request.Amount };
            var result = await _commandDispatcher.Send<CapturePaymentCommand, PaymentResultDto>(command);
            return Ok(result);
        }

        [HttpPost("{id:guid}/void")]
        public async Task<IActionResult> Void(Guid id)
        {
            var command = new VoidPaymentCommand { PaymentId = id };
            var result = await _commandDispatcher.Send<VoidPaymentCommand, PaymentResultDto>(command);
            return Ok(result);
        }

        [HttpPost("{id:guid}/refund")]
        public async Task<IActionResult> Refund(Guid id, [FromBody] RefundPaymentRequest request)
        {
            var command = new RefundPaymentCommand
            {
                PaymentId = id,
                Amount = request.Amount,
                Reason = request.Reason,
                IdempotencyKey = request.IdempotencyKey
            };
            var result = await _commandDispatcher.Send<RefundPaymentCommand, RefundResultDto>(command);
            return Ok(result);
        }

        [HttpGet("refunds")]
        public async Task<IActionResult> GetRefunds(
            [FromQuery] Guid? paymentId = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = new GetAdminRefundsQuery
            {
                PaymentId = paymentId,
                Status = status,
                Page = page,
                PageSize = pageSize
            };
            var result = await _queryDispatcher.Send<GetAdminRefundsQuery, PagedResult<AdminRefundDto>>(query);
            return Ok(result);
        }

        [HttpGet("refunds/{id:guid}")]
        public async Task<IActionResult> GetRefundById(Guid id)
        {
            var query = new GetAdminRefundByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminRefundByIdQuery, AdminRefundDto>(query);
            return Ok(result);
        }
    }

    public class CapturePaymentRequest
    {
        public decimal? Amount { get; set; }
    }

    public class RefundPaymentRequest
    {
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}