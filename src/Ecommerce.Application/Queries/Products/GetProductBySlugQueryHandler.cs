using System.Linq;
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
    public class GetProductBySlugQueryHandler : IQueryHandler<GetProductBySlugQuery, ProductDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly IPromotionEvaluationService? _promotionEvaluator;

        public GetProductBySlugQueryHandler(
            IApplicationDbContext db,
            IMapper mapper,
            IPromotionEvaluationService? promotionEvaluator = null)
        {
            _db = db;
            _mapper = mapper;
            _promotionEvaluator = promotionEvaluator;
        }

        public async Task<ProductDto> Handle(GetProductBySlugQuery query, CancellationToken cancellationToken = default)
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
                .FirstOrDefaultAsync(p => p.Slug == query.Slug, cancellationToken);


            // Unpublished or soft-deleted products must be unreachable by slug too.
            if (product == null || (!query.IncludeUnpublished && (product.IsDeleted || !product.IsActive)))
                throw new NotFoundException("Product", query.Slug);

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
