using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ecommerce.Infrastructure.Repositories
{
    // Simple placeholder - implementations should use EF Core DbContext
    public class GenericRepository<T> where T : class
    {
        public Task<T?> GetAsync(Guid id) => Task.FromResult<T?>(null);
        public Task<IEnumerable<T>> ListAsync() => Task.FromResult<IEnumerable<T>>(Enumerable.Empty<T>());
        public Task AddAsync(T entity) => Task.CompletedTask;
        public Task UpdateAsync(T entity) => Task.CompletedTask;
        public Task DeleteAsync(T entity) => Task.CompletedTask;
    }
}
