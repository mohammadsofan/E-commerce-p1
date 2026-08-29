using System;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Application.Tests
{
    public class DummySearchService : IProductSearchService
    {
        public Task IndexProductAsync(Guid productId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveFromIndexAsync(Guid productId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RebuildIndexAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ProductSearchResponse> SearchAsync(string searchTerm, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) => Task.FromResult(new ProductSearchResponse());
    }

    public class DummyConfig : Microsoft.Extensions.Configuration.IConfiguration
    {
        public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string key) => null!;
        public System.Collections.Generic.IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren() => null!;
        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => null!;
        public string this[string key] { get => null!; set { } }
    }
}
