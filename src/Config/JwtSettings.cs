using System.ComponentModel.DataAnnotations;

namespace reg.Settings
{
    public class JwtSettings
    {
        [Required]
        public string SecretKey { get; set; } = String.Empty;
        [Required]
        public int ExpiryMinutes { get; set; }
        [Required]
        public string Issuer { get; set; } = String.Empty;
        [Required]
        public string Audience { get; set; } = String.Empty;
    }
}