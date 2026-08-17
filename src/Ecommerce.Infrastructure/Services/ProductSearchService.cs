using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Services
{
    /// <summary>
    /// Product search backed by the denormalized ProductSearchDocument table.
    /// Relevance ranking: exact name match, then name prefix, then name contains,
    /// then SKU match, then description contains. The document table isolates
    /// search reads from the product write path and can be replaced by an
    /// external index (Elasticsearch/Solr) by swapping this implementation.
    /// </summary>
    public class ProductSearchService : IProductSearchService
    {
        private readonly Ecommerce.Application.Interfaces.IApplicationDbContext _db;

        public ProductSearchService(Ecommerce.Application.Interfaces.IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task IndexProductAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
            if (product == null)
                return;

            var document = await _db.ProductSearchDocuments
                .FirstOrDefaultAsync(d => d.ProductId == productId, cancellationToken);

            if (document == null)
            {
                document = new ProductSearchDocument
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id
                };
                _db.ProductSearchDocuments.Add(document);
            }

            document.Name = product.Name;
            document.Slug = product.Slug;
            document.Sku = product.Sku;
            document.ShortDescription = product.ShortDescription;
            document.BasePrice = product.BasePrice;
            document.IsActive = product.IsActive;
            document.IsDeleted = product.IsDeleted;
            document.SearchText = BuildSearchText(product);
            document.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveFromIndexAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var document = await _db.ProductSearchDocuments
                .FirstOrDefaultAsync(d => d.ProductId == productId, cancellationToken);
            if (document == null)
                return;

            _db.ProductSearchDocuments.Remove(document);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task RebuildIndexAsync(CancellationToken cancellationToken = default)
        {
            var products = await _db.Products.AsNoTracking().ToListAsync(cancellationToken);

            var existing = await _db.ProductSearchDocuments.ToListAsync(cancellationToken);
            _db.ProductSearchDocuments.RemoveRange(existing);

            foreach (var product in products)
            {
                _db.ProductSearchDocuments.Add(new ProductSearchDocument
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Name = product.Name,
                    Slug = product.Slug,
                    Sku = product.Sku,
                    ShortDescription = product.ShortDescription,
                    BasePrice = product.BasePrice,
                    IsActive = product.IsActive,
                    IsDeleted = product.IsDeleted,
                    SearchText = BuildSearchText(product),
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<ProductSearchResponse> SearchAsync(string searchTerm, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            page = System.Math.Max(1, page);
            pageSize = System.Math.Clamp(pageSize, 1, 100);

            var term = searchTerm?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(term))
            {
                return new ProductSearchResponse { Items = new List<ProductSearchHit>(), TotalCount = 0, Page = page, PageSize = pageSize };
            }

            var lower = term.ToLowerInvariant();

            var all = await _db.ProductSearchDocuments
                .AsNoTracking()
                .Where(d => !d.IsDeleted)
                .ToListAsync(cancellationToken);

            var scored = all
                .Select(d => new { Document = d, Score = Score(d, lower) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Document.Name)
                .ToList();

            var active = scored.Where(x => x.Document.IsActive).ToList();
            var total = active.Count;

            var results = active
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ProductSearchHit
                {
                    ProductId = x.Document.ProductId,
                    Name = x.Document.Name,
                    Slug = x.Document.Slug,
                    BasePrice = x.Document.BasePrice,
                    Score = x.Score
                })
                .ToList();

            return new ProductSearchResponse { Items = results, TotalCount = total, Page = page, PageSize = pageSize };
        }

        private static double Score(ProductSearchDocument document, string lowerTerm)
        {
            var name = document.Name.ToLowerInvariant();
            var slug = document.Slug.ToLowerInvariant();
            var sku = document.Sku.ToLowerInvariant();
            var description = document.ShortDescription.ToLowerInvariant();

            if (name == lowerTerm) return 100.0;
            if (name.StartsWith(lowerTerm)) return 90.0;
            if (name.Contains(lowerTerm)) return 80.0;
            if (slug.Contains(lowerTerm)) return 70.0;
            if (sku.Contains(lowerTerm)) return 60.0;
            if (description.Contains(lowerTerm)) return 50.0;
            if (document.SearchText.Contains(lowerTerm)) return 20.0;
            return 0.0;
        }

        private static string BuildSearchText(Product product)
        {
            return string.Join(" ", new[]
            {
                product.Name,
                product.Slug,
                product.Sku,
                product.ShortDescription,
                product.Description,
                product.SeoKeywords
            }).ToLowerInvariant();
        }
    }
}