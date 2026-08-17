using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Infrastructure.Persistence;

namespace Ecommerce.Infrastructure.Repositories
{
    public class EfRepository<T> where T : class
    {
        private readonly ApplicationDbContext _db;

        public EfRepository(ApplicationDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<T?> GetAsync(Guid id)
        {
            return await _db.Set<T>().FindAsync(id);
        }

        public async Task<List<T>> ListAsync()
        {
            return await _db.Set<T>().ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            await _db.Set<T>().AddAsync(entity);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(T entity)
        {
            _db.Set<T>().Update(entity);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(T entity)
        {
            _db.Set<T>().Remove(entity);
            await _db.SaveChangesAsync();
        }
    }
}
