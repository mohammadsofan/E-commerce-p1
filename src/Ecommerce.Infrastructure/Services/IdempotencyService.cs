using System;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Services
{
    public class IdempotencyService : IIdempotencyService
    {
        private readonly ApplicationDbContext _db;

        public IdempotencyService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<(bool Found, string Response)> TryGetResponseAsync(string key)
        {
            var rec = await _db.IdempotencyKeys.FirstOrDefaultAsync(k => k.Key == key);
            if (rec == null) return (false, null);
            if (!string.IsNullOrEmpty(rec.ResponseData)) return (true, rec.ResponseData);
            return (true, null);
        }

        public async Task<bool> TryRegisterAsync(string key, string requestHash, Guid ownerId)
        {
            var exists = await _db.IdempotencyKeys.AnyAsync(k => k.Key == key);
            if (exists) return false;

            var rec = new IdempotencyKey
            {
                Id = Guid.NewGuid(),
                Key = key,
                RequestHash = requestHash,
                OwnerId = ownerId,
                Status = "Registered",
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _db.IdempotencyKeys.AddAsync(rec);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task SaveResponseAsync(string key, string response)
        {
            var rec = await _db.IdempotencyKeys.FirstOrDefaultAsync(k => k.Key == key);
            if (rec == null) throw new InvalidOperationException("Idempotency key not found");
            rec.ResponseData = response;
            rec.Status = "Completed";
            await _db.SaveChangesAsync();
        }
    }
}
