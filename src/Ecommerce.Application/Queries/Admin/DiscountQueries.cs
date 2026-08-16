using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetAdminCouponsQuery : IQuery<PagedResult<AdminCouponDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public string? Type { get; set; }
    }

    public class GetAdminCouponByIdQuery : IQuery<AdminCouponDto>
    {
        public Guid Id { get; set; }
    }

    public class GetAdminCouponByCodeQuery : IQuery<AdminCouponDto>
    {
        public string Code { get; set; } = string.Empty;
    }

    public class GetAdminPromotionsQuery : IQuery<PagedResult<AdminPromotionDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public string? Type { get; set; }
    }

    public class GetAdminPromotionByIdQuery : IQuery<AdminPromotionDto>
    {
        public Guid Id { get; set; }
    }

    public class ValidateCouponQuery : IQuery<ValidateCouponResponse>
    {
        public string Code { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public decimal OrderTotal { get; set; }
        public List<Guid> ProductIds { get; set; } = new();
        public List<Guid> CategoryIds { get; set; } = new();
    }

    public class CalculateDiscountsQuery : IQuery<DiscountCalculationResult>
    {
        public Guid UserId { get; set; }
        public decimal Subtotal { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
        public string? CouponCode { get; set; }
    }
}