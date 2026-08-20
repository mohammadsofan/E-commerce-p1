using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Identity;
using Ecommerce.Infrastructure.Services;
using Ecommerce.Infrastructure.Auth;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ecommerce.IntegrationTests
{
    public class RefreshTokenIntegrationTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private UserManager<ApplicationUser> CreateUserManager(ApplicationDbContext ctx)
        {
            var store = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, Guid>(ctx);
            var options = Options.Create(new IdentityOptions());
            var pwdHasher = new PasswordHasher<ApplicationUser>();
            var userValidators = new IUserValidator<ApplicationUser>[] { new UserValidator<ApplicationUser>() };
            var pwdValidators = new IPasswordValidator<ApplicationUser>[] { new PasswordValidator<ApplicationUser>() };
            var normalizer = new UpperInvariantLookupNormalizer();
            var errorDescriber = new IdentityErrorDescriber();
            var logger = new NullLogger<UserManager<ApplicationUser>>();

            return new UserManager<ApplicationUser>(store, options, pwdHasher, userValidators, pwdValidators, normalizer, errorDescriber, null, logger);
        }

        private class FakeTokenService : ITokenService
        {
            public Task<string> CreateTokenAsync(ApplicationUserDto user)
            {
                return Task.FromResult($"token-for-{user.UserName}");
            }
        }

        private static string ComputeHash(string token)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            return Convert.ToHexString(sha.ComputeHash(bytes));
        }

        [Fact]
        public async Task RefreshToken_Lifecycle_Create_Refresh_Revoke_RevokeAll_RemoveExpired()
        {
            using var ctx = CreateInMemoryContext();

            var userManager = CreateUserManager(ctx);
            var tokenService = new FakeTokenService();
            var svc = new RefreshTokenService(ctx, tokenService, userManager);

            // Create user
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "tester",
                Email = "tester@example.com",
                FirstName = "Test",
                LastName = "User",
                DisplayName = "Test User",
                ProfileImageUrl = "",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            var createResult = await userManager.CreateAsync(user);
            Assert.True(createResult.Succeeded);

            // Create refresh token
            var (token1, expires1) = await svc.CreateRefreshTokenAsync(user.Id);
            Assert.False(string.IsNullOrEmpty(token1));
            Assert.True(expires1 > DateTimeOffset.UtcNow);

            var hash1 = ComputeHash(token1);
            var dbToken1 = await ctx.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash1);
            Assert.NotNull(dbToken1);
            Assert.Equal(user.Id, dbToken1.UserId);
            Assert.True(dbToken1.IsActive);

            // Refresh (rotation)
            var refreshResult = await svc.RefreshAsync(token1);
            Assert.True(refreshResult.Success);
            Assert.False(string.IsNullOrEmpty(refreshResult.RefreshToken));
            Assert.False(string.IsNullOrEmpty(refreshResult.AccessToken));
            Assert.NotNull(refreshResult.ExpiresAt);
            Assert.True(refreshResult.ExpiresAt > DateTimeOffset.UtcNow);

            // Old token should be revoked and replaced reference set
            var dbOld = await ctx.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash1);
            Assert.NotNull(dbOld);
            Assert.NotNull(dbOld.RevokedAt);
            Assert.NotNull(dbOld.ReplacedByTokenHash);

            // Revoke new token
            var hashNew = ComputeHash(refreshResult.RefreshToken);
            var revokeRes = await svc.RevokeAsync(refreshResult.RefreshToken);
            Assert.True(revokeRes);
            var dbNew = await ctx.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hashNew);
            Assert.NotNull(dbNew);
            Assert.NotNull(dbNew.RevokedAt);

            // Create another token and test RevokeAll
            var (token2, _) = await svc.CreateRefreshTokenAsync(user.Id);
            var revokeAllRes = await svc.RevokeAllAsync(user.Id);
            Assert.True(revokeAllRes);
            var tokens = await ctx.RefreshTokens.Where(x => x.UserId == user.Id).ToListAsync();
            Assert.All(tokens, t => Assert.NotNull(t.RevokedAt));

            // Add expired token and remove
            var expired = new Domain.Entities.RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = ComputeHash("expired-token"),
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-40),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(-10)
            };
            await ctx.RefreshTokens.AddAsync(expired);
            await ctx.SaveChangesAsync();

            var removed = await svc.RemoveExpiredAsync();
            Assert.True(removed >= 1);
            var exists = await ctx.RefreshTokens.AnyAsync(x => x.Id == expired.Id);
            Assert.False(exists);
        }

        [Fact]
        public async Task RefreshAsync_ReusedRevokedToken_RevokesAllUserTokens()
        {
            using var ctx = CreateInMemoryContext();

            var userManager = CreateUserManager(ctx);
            var tokenService = new FakeTokenService();
            var svc = new RefreshTokenService(ctx, tokenService, userManager);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "reuse-tester",
                Email = "reuse@example.com",
                FirstName = "Reuse",
                LastName = "Tester",
                DisplayName = "Reuse Tester",
                ProfileImageUrl = "",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await userManager.CreateAsync(user);

            var (token1, _) = await svc.CreateRefreshTokenAsync(user.Id);
            var refreshResult = await svc.RefreshAsync(token1);
            Assert.True(refreshResult.Success);

            // Attempt to reuse the now-revoked token1
            var reuseResult = await svc.RefreshAsync(token1);
            Assert.False(reuseResult.Success);

            // All tokens for the user should be revoked after reuse detection
            var tokens = await ctx.RefreshTokens.Where(x => x.UserId == user.Id).ToListAsync();
            Assert.NotEmpty(tokens);
            Assert.All(tokens, t => Assert.NotNull(t.RevokedAt));
        }

        [Fact]
        public async Task RefreshAsync_IssuedAccessToken_IncludesUserRoles()
        {
            using var ctx = CreateInMemoryContext();

            var userManager = CreateUserManager(ctx);
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Jwt:Key"] = "test_signing_key_that_is_long_enough_for_hmac_sha256_1234",
                    ["Jwt:Issuer"] = "ecommerce-test"
                })
                .Build();
            var tokenService = new JwtTokenService(config);
            var svc = new RefreshTokenService(ctx, tokenService, userManager);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "admin-refresh-tester",
                Email = "admin-refresh@example.com",
                FirstName = "Admin",
                LastName = "Refresher",
                DisplayName = "Admin Refresher",
                ProfileImageUrl = "",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await userManager.CreateAsync(user);

            var role = new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = "Admin",
                NormalizedName = "ADMIN",
                Description = "Admin role",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await ctx.Roles.AddAsync(role);
            await ctx.SaveChangesAsync();
            await userManager.AddToRoleAsync(user, "Admin");

            var (token1, _) = await svc.CreateRefreshTokenAsync(user.Id);
            var refreshResult = await svc.RefreshAsync(token1);
            Assert.True(refreshResult.Success);
            Assert.False(string.IsNullOrEmpty(refreshResult.AccessToken));

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(refreshResult.AccessToken);
            var roleClaims = jwt.Claims.Where(c => c.Type == "role" || c.Type == ClaimTypes.Role).ToList();
            Assert.Contains(roleClaims, c => c.Value == "Admin");
        }
    }
}
