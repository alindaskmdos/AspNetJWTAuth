using reg.Models;

namespace reg.Services.Interfaces
{
    public interface ITokenService
    {
        Task<string> GenerateAccessTokenAsync(User user);
        Task<string> GenerateRefreshTokenAsync(User user);

        Task<(string accessToken, string refreshToken)> RefreshAccessTokenAsync(string email, string refreshToken);
        Task<bool> ValidateAccessTokenAsync(string accessToken);

        Task<bool> ValidateRefreshTokenAsync(User user, string refreshToken);
        Task<bool> RevokeRefreshTokenAsync(User user, string refreshToken);
    }
}
