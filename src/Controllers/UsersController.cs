using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using reg.Models;
using reg.Data.Repositories;
using reg.Services;
using reg.Models.DTOs;

namespace reg.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserRepository _userRepository;
        private readonly UserManager<User> _userManager;
        private readonly TokenService _tokenService;

        public UsersController(
            UserRepository userRepository,
            UserManager<User> userManager,
            ILogger<UsersController> logger,
            TokenService tokenService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
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

        [HttpGet("{id}")]
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

        [HttpPut("{id}/profile")]
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

        // добавить сюда код на почту для смены пароля
        [HttpPut("change-password")]
        [Authorize(Roles = "Admin, User")]
        public async Task<IActionResult> ChangeUserPassword(ChangePasswordDto changePasswordDto)
        {
            try
            {
                var currentUserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var isAdmin = User.IsInRole("Admin");

                if (!isAdmin && currentUserEmail != changePasswordDto.Email)
                    return Forbid("У вас нет прав на изменение пароля этого пользователя");

                await _userRepository.ChangeUserPassword(changePasswordDto);

                return Ok("Пароль успешно изменен");
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

        [HttpDelete("{id}")]
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

        [HttpPost("{id}/lock")]
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

        [HttpPost("{id}/unlock")]
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