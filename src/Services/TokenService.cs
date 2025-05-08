using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using reg.Models;
using reg.Settings;
using reg.Services.Interfaces;
using reg.Data.Repositories;


namespace reg.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly RefreshTokenSettings _refreshTokenSettings;
        private readonly TokenRepository _tokenRepository;
        private readonly UserManager<User> _userManager;

        public TokenService(
            IOptions<JwtSettings> jwtSettings,
            IOptions<RefreshTokenSettings> refreshTokenSettings,
            TokenRepository tokenRepository,
            UserManager<User> userManager)
        {
            _jwtSettings = jwtSettings.Value;
            _refreshTokenSettings = refreshTokenSettings.Value;
            _tokenRepository = tokenRepository;
            _userManager = userManager;
        }

        public async Task<string> GenerateAccessTokenAsync(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("CreatedAt", DateTime.UtcNow.ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        public async Task<string> GenerateRefreshTokenAsync(User user)
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] randomBytes = new byte[_refreshTokenSettings.TokenSizeBytes];
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        public async Task<(string accessToken, string refreshToken)> RefreshAccessTokenAsync(string email, string refreshToken)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                throw new InvalidOperationException("Пользователь не найден");
            }

            var (isValid, _) = await _tokenRepository.ValidateRefreshTokenByEmailAsync(email, refreshToken);
            if (!isValid)
            {
                throw new InvalidOperationException("Недействительный refresh токен");
            }

            string newAccessToken = await GenerateAccessTokenAsync(user);
            string newRefreshToken = await GenerateRefreshTokenAsync(user);

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenSettings.ExpiryDays)
            };

            await _tokenRepository.AddRefreshTokenAsync(refreshTokenEntity);

            return (newAccessToken, newRefreshToken);
        }

        public async Task<bool> ValidateAccessTokenAsync(string accessToken)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                tokenHandler.ValidateToken(accessToken, validationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ValidateRefreshTokenAsync(User user, string refreshToken)
        {
            return await _tokenRepository.ValidateRefreshTokenAsync(user.Id, refreshToken, true);
        }

        public async Task<bool> RevokeRefreshTokenAsync(User user, string refreshToken)
        {
            return await _tokenRepository.RevokeRefreshTokenAsync(user.Id, refreshToken);
        }
    }
}