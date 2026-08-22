using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces
{
    public class ProductPromotionTarget
    {
        public Guid ProductId { get; set; }
        public Guid? CategoryId { get; set; }
        public decimal BasePrice { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class ProductPromotionEvaluation
    {
        public Guid ProductId { get; set; }
        public decimal BasePrice { get; set; }
        public decimal PromotionalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalDiscount { get; set; }
        public int DiscountPercentage { get; set; }
        public bool HasActivePromotion { get; set; }
        public string? PromotionName { get; set; }
        public string? PromotionBadge { get; set; }
        public Guid? PromotionId { get; set; }
    }

    public class CartLevelPromotionTarget
    {
        public Guid ProductId { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }

    public class CartLevelPromotionResult
    {
        public bool HasCartLevelPromotion { get; set; }
        public decimal TotalCartDiscount { get; set; }
        public string? PromotionName { get; set; }
        public Guid? PromotionId { get; set; }
        public Guid? SuggestedFreeGiftProductId { get; set; }
    }

    public interface IPromotionEvaluationService
    {
        Task<ProductPromotionEvaluation> EvaluateProductAsync(Guid productId, Guid? categoryId, decimal basePrice, CancellationToken cancellationToken = default);
        Task<ProductPromotionEvaluation> EvaluateProductAsync(Guid productId, Guid? categoryId, decimal basePrice, int quantity, CancellationToken cancellationToken = default);
        Task<Dictionary<Guid, ProductPromotionEvaluation>> EvaluateProductsAsync(IEnumerable<ProductPromotionTarget> targets, CancellationToken cancellationToken = default);
        Task<CartLevelPromotionResult> EvaluateCartLevelPromotionsAsync(List<CartLevelPromotionTarget> cartItems, decimal currentSubtotal, CancellationToken cancellationToken = default);
    }
}
