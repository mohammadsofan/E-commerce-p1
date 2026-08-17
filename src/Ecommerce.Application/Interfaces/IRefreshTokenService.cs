using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces
{
    public interface IRefreshTokenService
    {
        Task<(string Token, System.DateTimeOffset ExpiresAt)> CreateRefreshTokenAsync(System.Guid userId);
        Task<(bool Success, string? AccessToken, string? RefreshToken, System.DateTimeOffset? ExpiresAt)> RefreshAsync(string refreshToken);
        Task<bool> RevokeAsync(string refreshToken);
        Task<bool> RevokeAllAsync(System.Guid userId);
        Task<int> RemoveExpiredAsync();
    }
}
