using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using reg.Models;
using reg.Data.Repositories;
using reg.Services;
using reg.Models.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace reg.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserRepository _userRepository;
        private readonly UserManager<User> _userManager;
        private readonly TokenService _tokenService;
        private readonly EmailConfirmationService _emailConfirmationService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            UserRepository userRepository,
            UserManager<User> userManager,
            TokenService tokenService,
            EmailConfirmationService emailConfirmationService,
            ILogger<UsersController> logger)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _tokenService = tokenService;
            _emailConfirmationService = emailConfirmationService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userRepository.GetAllUsers();
                var userDtos = new List<UserResponseDto>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);

                    var userDto = new UserResponseDto
                    {
                        Id = Guid.Parse(user.Id),
                        Email = user.Email ?? string.Empty,
                        FirstName = user.FirstName ?? string.Empty,
                        LastName = user.LastName ?? string.Empty,
                        Roles = roles.ToList(),
                        CreatedAt = user.CreatedAt,
                        IsLocked = lockoutEnd != null && lockoutEnd > DateTime.UtcNow
                    };
                    userDtos.Add(userDto);
                }


                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _userRepository.GetUserByEmail(forgotPasswordDto.Email);
                if (user == null)
                {
                    return BadRequest();
                }

                var passwordResetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _emailConfirmationService.SendResetPasswordEmailAsync(forgotPasswordDto.Email, passwordResetToken);

                return Ok("Письмо для сброса пароля отправлено на ваш email");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке письма для сброса пароля для email: {Email}", forgotPasswordDto.Email);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _userRepository.GetUserByEmail(resetPasswordDto.Email);
                if (user == null)
                    return BadRequest("Неверный токен или email");

                var result = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest($"Не удалось сбросить пароль: {errors}");
                }

                _logger.LogInformation("Пароль успешно сброшен для пользователя: {Email}", resetPasswordDto.Email);
                return Ok("Пароль успешно сброшен");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сбросе пароля для email: {Email}", resetPasswordDto.Email);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        [HttpPut("update-email")]
        [Authorize]
        public async Task<IActionResult> UpdateUserEmail(UpdateUserDto userDto)
        {
            try
            {
                if (string.IsNullOrEmpty(userDto.Email))
                    return BadRequest("Email обязателен для обновления пользователя");

                var currentUserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var isAdmin = User.IsInRole("Admin");

                if (!isAdmin && currentUserEmail != userDto.Email)
                    return Forbid("У вас нет прав на изменение email этого пользователя");

                string accessToken = string.Empty;
                string refreshToken = string.Empty;
                User? user = await _userRepository.GetUserByEmail(userDto.Email);
                if (user == null)
                    return NotFound($"Пользователь с email {userDto.Email} не найден");

                if (!string.IsNullOrEmpty(userDto.NewEmail))
                {
                    user = await _userRepository.ChangeUserEmail(user.Email ?? string.Empty, userDto.NewEmail);
                    accessToken = await _tokenService.GenerateAccessTokenAsync(user);
                    refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);
                }

                var userResponse = new UserResponseDto
                {
                    Id = Guid.Parse(user.Id),
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    CreatedAt = user.CreatedAt
                };

                return Ok(new { accessToken, refreshToken, userResponse });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }
        [HttpPut("change-password")]
        [Authorize(Roles = "Admin, User")]
        public async Task<IActionResult> ChangeUserPassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var isAdmin = User.IsInRole("Admin");

            var user = await _userRepository.GetUserByEmail(changePasswordDto.Email);
            if (user == null)
            {
                return NotFound($"Пользователь с email {changePasswordDto.Email} не найден");
            }

            if (!isAdmin && currentUserEmail != changePasswordDto.Email)
                return Forbid("У вас нет прав на изменение пароля этого пользователя");

            var passwordValid = await _userManager.CheckPasswordAsync(user, changePasswordDto.CurrentPassword);
            if (!passwordValid)
                return BadRequest("Неверный текущий пароль");

            var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest($"Не удалось изменить пароль: {errors}");
            }

            return Ok("Пароль успешно изменен");
        }

        [HttpGet("by-email")]
        public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
        {
            try
            {
                User? user = await _userRepository.GetUserByEmail(email);
                if (user == null)
                    return NotFound($"Пользователь с email {email} не найден");

                var roles = await _userManager.GetRolesAsync(user);
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);

                var userDto = new UserResponseDto
                {
                    Id = Guid.Parse(user.Id),
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    Roles = roles.ToList(),
                    CreatedAt = user.CreatedAt,
                    IsLocked = lockoutEnd != null && lockoutEnd > DateTime.UtcNow
                };

                return Ok(userDto);
            }
            catch (ArgumentNullException)
            {
                return BadRequest("Email не может быть пустым");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }

        [HttpGet("by-id/{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            try
            {
                var user = await _userRepository.GetUserById(id);
                if (user == null)
                    return NotFound($"Пользователь с ID {id} не найден");

                var roles = await _userManager.GetRolesAsync(user);
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);

                var userDto = new UserResponseDto
                {
                    Id = Guid.Parse(user.Id),
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    Roles = roles.ToList(),
                    CreatedAt = user.CreatedAt,
                    IsLocked = lockoutEnd != null && lockoutEnd > DateTime.UtcNow
                };

                return Ok(userDto);
            }
            catch (ArgumentNullException)
            {
                return BadRequest("ID пользователя не может быть пустым");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }

        [HttpPut("by-id/{id}/profile")]
        [Authorize]
        public async Task<IActionResult> UpdateUserProfile(string id, UpdateUserProfileDto profileDto)
        {
            try
            {
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin");

                if (!isAdmin && currentUserId != id)
                    return Forbid("У вас нет прав на обновление этого профиля");

                var user = await _userRepository.UpdateUserProfile(id, profileDto);

                var userResponse = new UserResponseDto
                {
                    Id = Guid.Parse(user.Id),
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    CreatedAt = user.CreatedAt
                };

                return Ok(userResponse);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }

        [HttpDelete("by-id/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUserById(string id)
        {
            try
            {
                bool success = await _userRepository.DeleteUserById(id);
                if (success)
                    return Ok($"Пользователь с ID {id} успешно удален");
                else
                    return BadRequest($"Не удалось удалить пользователя с ID {id}");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }

        [HttpDelete("by-email")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUserByEmail([FromQuery] string email)
        {
            try
            {
                bool success = await _userRepository.DeleteUser(email);
                if (success)
                    return Ok($"Пользователь с email {email} успешно удален");
                else
                    return BadRequest($"Не удалось удалить пользователя с email {email}");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }

        [HttpPost("by-id/{id}/lock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> LockUser(string id)
        {
            try
            {
                bool success = await _userRepository.LockUser(id);
                if (success)
                    return Ok($"Пользователь с ID {id} заблокирован");
                else
                    return BadRequest($"Не удалось заблокировать пользователя с ID {id}");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }

        [HttpPost("by-id/{id}/unlock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnlockUser(string id)
        {
            try
            {
                bool success = await _userRepository.UnlockUser(id);
                if (success)
                    return Ok($"Пользователь с ID {id} разблокирован");
                else
                    return BadRequest($"Не удалось разблокировать пользователя с ID {id}");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }
    }
}