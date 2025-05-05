using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using reg.Models;

namespace reg.Data.Repositories
{
    public class TokenRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public TokenRepository(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<(bool isValid, User? user)> ValidateRefreshTokenAsync(string userId, string refreshToken)
        {
            RefreshToken? rt = await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(u => u.UserId == userId
                    && u.Token == refreshToken
                    && u.ExpiresAt > DateTime.UtcNow);

            return (rt != null, rt?.User);
        }

        public async Task<string> getIpAddressByRefreshToken(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            return token?.IpAddress ?? string.Empty;
        }

        public async Task<(bool isValid, User? user)> ValidateRefreshTokenByEmailAsync(string email, string refreshToken)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return (false, null);

            return await ValidateRefreshTokenAsync(user.Id, refreshToken);
        }

        public async Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken, bool returnOnlyStatus)
        {
            var (isValid, _) = await ValidateRefreshTokenAsync(userId, refreshToken);
            return isValid;
        }

        public async Task<bool> RevokeRefreshTokenAsync(string userId, string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == refreshToken);

            if (token == null)
                return false;

            _context.RefreshTokens.Remove(token);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RevokeRefreshTokenByEmailAsync(string email, string refreshToken)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            return await RevokeRefreshTokenAsync(user.Id, refreshToken);
        }

        public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            var userTokens = await _context.RefreshTokens
                .Where(t => t.UserId == refreshToken.UserId)
                .ToListAsync();

            if (userTokens.Count >= 5)
            {
                var oldestToken = userTokens
                    .OrderBy(t => t.CreatedAt)
                    .FirstOrDefault();

                if (oldestToken != null)
                {
                    _context.RefreshTokens.Remove(oldestToken);
                }
            }

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
        }
    }
}
