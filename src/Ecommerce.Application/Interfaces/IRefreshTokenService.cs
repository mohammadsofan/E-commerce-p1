using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces
{
    public interface IRefreshTokenService
    {
        Task<(string Token, System.DateTimeOffset ExpiresAt)> CreateRefreshTokenAsync(System.Guid userId);
        Task<(bool Success, string? AccessToken, string? RefreshToken)> RefreshAsync(string refreshToken);
        Task<bool> RevokeAsync(string refreshToken);
    }
}
