using System.ComponentModel.DataAnnotations;

namespace reg.Models.DTOs
{
    public class UserRoleDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;
    }
}