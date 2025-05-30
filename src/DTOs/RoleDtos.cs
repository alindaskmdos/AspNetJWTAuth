using System.ComponentModel.DataAnnotations;

namespace reg.Models.DTOs
{
    public class UserRoleDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;
    }
}