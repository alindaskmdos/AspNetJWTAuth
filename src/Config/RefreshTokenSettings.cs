using System.ComponentModel.DataAnnotations;

namespace reg.Settings
{
    public class RefreshTokenSettings
    {
        [Required]
        [Range(1, 365)]
        public int ExpiryDays { get; set; }

        [Required]
        [Range(32, 128)]
        public int TokenSizeBytes { get; set; }

        [Required]
        [Range(1, 10)]
        public int MaxActiveTokensPerUser { get; set; }
    }
}