using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.DTOs;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly Persistence.ApplicationDbContext _db;
        private readonly ITokenService _tokenService;
        private readonly UserManager<ApplicationUser> _userManager;

        public RefreshTokenService(Persistence.ApplicationDbContext db, ITokenService tokenService, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _tokenService = tokenService;
            _userManager = userManager;
        }

        public async Task<(string Token, DateTimeOffset ExpiresAt)> CreateRefreshTokenAsync(Guid userId)
        {
            var token = GenerateToken();
            var hash = ComputeHash(token);
            var now = DateTimeOffset.UtcNow;
            var expires = now.AddDays(30);

            var entity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = hash,
                CreatedAt = now,
                ExpiresAt = expires
            };

            _db.RefreshTokens.Add(entity);
            await _db.SaveChangesAsync();

            return (token, expires);
        }

        public async Task<(bool Success, string? AccessToken, string? RefreshToken, DateTimeOffset? ExpiresAt)> RefreshAsync(string refreshToken)
        {
            var hash = ComputeHash(refreshToken);
            var entity = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
            if (entity == null) return (false, null, null, null);

            if (entity.RevokedAt != null)
            {
                // Reuse of a revoked token may indicate theft — invalidate all sessions for this user.
                await RevokeAllAsync(entity.UserId);
                return (false, null, null, null);
            }

            if (entity.IsExpired) return (false, null, null, null);

            var user = await _userManager.FindByIdAsync(entity.UserId.ToString());
            if (user == null) return (false, null, null, null);

            entity.RevokedAt = DateTimeOffset.UtcNow;

            var (newToken, expires) = await CreateRefreshTokenAsync(entity.UserId);
            entity.ReplacedByTokenHash = ComputeHash(newToken);

            await _db.SaveChangesAsync();

            var roles = (await _userManager.GetRolesAsync(user)).ToList();
            var dto = new ApplicationUserDto { Id = user.Id, Email = user.Email ?? string.Empty, UserName = user.UserName ?? string.Empty, Roles = roles };
            var accessToken = await _tokenService.CreateTokenAsync(dto);

            return (true, accessToken, newToken, expires);
        }

        public async Task<bool> RevokeAsync(string refreshToken)
        {
            var hash = ComputeHash(refreshToken);
            var entity = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
            if (entity == null || !entity.IsActive) return false;
            entity.RevokedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RevokeAllAsync(Guid userId)
        {
            var tokens = await _db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync();
            if (!tokens.Any()) return false;
            var now = DateTimeOffset.UtcNow;
            foreach (var t in tokens) t.RevokedAt = now;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<int> RemoveExpiredAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var expired = await _db.RefreshTokens.Where(x => x.ExpiresAt <= now).ToListAsync();
            if (!expired.Any()) return 0;
            _db.RefreshTokens.RemoveRange(expired);
            await _db.SaveChangesAsync();
            return expired.Count;
        }

        private static string GenerateToken()
        {
            var random = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(random);
        }

        private static string ComputeHash(string token)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
