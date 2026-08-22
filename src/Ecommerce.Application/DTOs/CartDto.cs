using System;
using System.Collections.Generic;

namespace Ecommerce.Application.DTOs
{
    public class CartDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        public decimal TotalAmount { get; set; }
        public string? AppliedCouponCode { get; set; }
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
    }

    public class CartItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string ProductSlug { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string? ImageUrl { get; set; }
        public string? SelectedOptions { get; set; }
        public string? VariantName { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal? PromotionalPrice { get; set; }
        public string? PromotionName { get; set; }
        public string? PromotionBadge { get; set; }
    }
}
