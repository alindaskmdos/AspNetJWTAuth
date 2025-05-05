using Microsoft.AspNetCore.Identity;

namespace reg.Models
{
    public class User : IdentityUser
    {
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}