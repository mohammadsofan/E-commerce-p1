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

        public async Task<(bool Success, string? AccessToken, string? RefreshToken)> RefreshAsync(string refreshToken)
        {
            var hash = ComputeHash(refreshToken);
            var entity = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
            if (entity == null || !entity.IsActive) return (false, null, null);

            // load user
            var user = await _userManager.FindByIdAsync(entity.UserId.ToString());
            if (user == null) return (false, null, null);

            // Revoke current and issue new
            entity.RevokedAt = DateTimeOffset.UtcNow;

            var (newToken, expires) = await CreateRefreshTokenAsync(entity.UserId);
            entity.ReplacedByTokenHash = ComputeHash(newToken);

            await _db.SaveChangesAsync();

            var dto = new ApplicationUserDto { Id = user.Id, Email = user.Email, UserName = user.UserName };
            var accessToken = await _tokenService.CreateTokenAsync(dto);

            return (true, accessToken, newToken);
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
