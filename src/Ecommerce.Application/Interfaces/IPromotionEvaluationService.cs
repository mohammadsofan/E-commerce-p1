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
    }

    public class ProductPromotionEvaluation
    {
        public Guid ProductId { get; set; }
        public decimal BasePrice { get; set; }
        public decimal PromotionalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public int DiscountPercentage { get; set; }
        public bool HasActivePromotion { get; set; }
        public string? PromotionName { get; set; }
        public string? PromotionBadge { get; set; }
        public Guid? PromotionId { get; set; }
    }

    public interface IPromotionEvaluationService
    {
        Task<ProductPromotionEvaluation> EvaluateProductAsync(Guid productId, Guid? categoryId, decimal basePrice, CancellationToken cancellationToken = default);
        Task<Dictionary<Guid, ProductPromotionEvaluation>> EvaluateProductsAsync(IEnumerable<ProductPromotionTarget> targets, CancellationToken cancellationToken = default);
    }
}
