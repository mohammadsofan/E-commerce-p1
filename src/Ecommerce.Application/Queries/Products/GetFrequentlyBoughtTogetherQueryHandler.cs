using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Products
{
    public class GetFrequentlyBoughtTogetherQueryHandler : IQueryHandler<GetFrequentlyBoughtTogetherQuery, List<ProductDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetFrequentlyBoughtTogetherQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<ProductDto>> Handle(GetFrequentlyBoughtTogetherQuery query, CancellationToken cancellationToken = default)
        {
            var inputIds = query.ProductIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
            var limit = Math.Clamp(query.Limit > 0 ? query.Limit : 4, 1, 50);

            var rankedProductIds = new List<Guid>();

            // 1. Co-occurrence Matrix from Order History
            if (inputIds.Count > 0)
            {
                var matchedOrderIds = await _db.OrderItems
                    .AsNoTracking()
                    .Where(oi => inputIds.Contains(oi.ProductId))
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                if (matchedOrderIds.Count > 0)
                {
                    var coOccurrences = await _db.OrderItems
                        .AsNoTracking()
                        .Where(oi => matchedOrderIds.Contains(oi.OrderId) && !inputIds.Contains(oi.ProductId))
                        .GroupBy(oi => oi.ProductId)
                        .Select(g => new { ProductId = g.Key, Frequency = g.Count() })
                        .OrderByDescending(x => x.Frequency)
                        .Take(limit)
                        .ToListAsync(cancellationToken);

                    rankedProductIds.AddRange(coOccurrences.Select(x => x.ProductId));
                }
            }

            // 2. Fallback: Category Affinity (products in same categories as cart items)
            if (rankedProductIds.Count < limit && inputIds.Count > 0)
            {
                var inputCategoryIds = await _db.Products
                    .AsNoTracking()
                    .Where(p => inputIds.Contains(p.Id) && p.CategoryId.HasValue)
                    .Select(p => p.CategoryId!.Value)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var excludedIds = inputIds.Concat(rankedProductIds).Distinct().ToList();
                var remainingSlots = limit - rankedProductIds.Count;

                if (inputCategoryIds.Count > 0 && remainingSlots > 0)
                {
                    var categoryFallbacks = await _db.Products
                        .AsNoTracking()
                        .Where(p => !p.IsDeleted && p.IsActive && !excludedIds.Contains(p.Id) && p.CategoryId.HasValue && inputCategoryIds.Contains(p.CategoryId.Value))
                        .OrderByDescending(p => p.IsFeatured)
                        .ThenByDescending(p => p.CreatedAt)
                        .Select(p => p.Id)
                        .Take(remainingSlots)
                        .ToListAsync(cancellationToken);

                    rankedProductIds.AddRange(categoryFallbacks);
                }
            }

            // 3. Fallback: Popular / Featured Catalog Items
            if (rankedProductIds.Count < limit)
            {
                var excludedIds = inputIds.Concat(rankedProductIds).Distinct().ToList();
                var remainingSlots = limit - rankedProductIds.Count;

                if (remainingSlots > 0)
                {
                    var generalFallbacks = await _db.Products
                        .AsNoTracking()
                        .Where(p => !p.IsDeleted && p.IsActive && !excludedIds.Contains(p.Id))
                        .OrderByDescending(p => p.IsFeatured)
                        .ThenByDescending(p => p.CreatedAt)
                        .Select(p => p.Id)
                        .Take(remainingSlots)
                        .ToListAsync(cancellationToken);

                    rankedProductIds.AddRange(generalFallbacks);
                }
            }

            if (rankedProductIds.Count == 0)
            {
                return new List<ProductDto>();
            }

            // Load full product details
            var products = await _db.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .Include(p => p.InventoryItems)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => rankedProductIds.Contains(p.Id) && !p.IsDeleted && p.IsActive)
                .ToListAsync(cancellationToken);

            // Maintain ranked co-occurrence order
            var productMap = products.ToDictionary(p => p.Id);
            var orderedProducts = rankedProductIds
                .Where(id => productMap.ContainsKey(id))
                .Select(id => productMap[id])
                .ToList();

            var dtos = _mapper.Map<List<ProductDto>>(orderedProducts);

            // Compute available stock
            for (var i = 0; i < orderedProducts.Count; i++)
            {
                var entity = orderedProducts[i];
                var dto = dtos[i];
                dto.AvailableStock = entity.InventoryItems.Sum(ii => ii.QuantityOnHand - ii.QuantityReserved);
            }

            return dtos;
        }
    }
}
