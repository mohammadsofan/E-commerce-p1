using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Products
{
    public class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ProductDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly IPromotionEvaluationService? _promotionEvaluator;

        public GetProductByIdQueryHandler(
            IApplicationDbContext db,
            IMapper mapper,
            IPromotionEvaluationService? promotionEvaluator = null)
        {
            _db = db;
            _mapper = mapper;
            _promotionEvaluator = promotionEvaluator;
        }

        public async Task<ProductDto> Handle(GetProductByIdQuery query, CancellationToken cancellationToken = default)
        {
            var product = await _db.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .Include(p => p.InventoryItems)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.VariantAttributes)
                        .ThenInclude(va => va.ProductAttribute)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.InventoryItems)
                .FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken);

            // A product that is unpublished or soft-deleted must not be reachable by GUID,
            // otherwise the whole catalog-visibility rule can be bypassed with a direct link.
            if (product == null || (!query.IncludeUnpublished && (product.IsDeleted || !product.IsActive)))
                throw new NotFoundException("Product", query.Id);

            var dto = _mapper.Map<ProductDto>(product);

            if (_promotionEvaluator != null)
            {
                var promoEval = await _promotionEvaluator.EvaluateProductAsync(
                    dto.Id,
                    dto.Category?.Id,
                    dto.BasePrice,
                    cancellationToken);

                if (promoEval.HasActivePromotion)
                {
                    dto.PromotionalPrice = promoEval.PromotionalPrice;
                    dto.DiscountPercentage = promoEval.DiscountPercentage;
                    dto.PromotionName = promoEval.PromotionName;
                    dto.PromotionBadge = promoEval.PromotionBadge;
                }
            }

            return dto;
        }
    }
}
