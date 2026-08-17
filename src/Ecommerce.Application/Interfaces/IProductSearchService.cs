using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces
{
    public class ProductSearchHit
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public double Score { get; set; }
    }

    public class ProductSearchResponse
    {
        public List<ProductSearchHit> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public interface IProductSearchService
    {
        Task IndexProductAsync(Guid productId, CancellationToken cancellationToken = default);
        Task RemoveFromIndexAsync(Guid productId, CancellationToken cancellationToken = default);
        Task RebuildIndexAsync(CancellationToken cancellationToken = default);
        Task<ProductSearchResponse> SearchAsync(string searchTerm, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    }
}