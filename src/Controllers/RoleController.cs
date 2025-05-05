using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using reg.Models;
using reg.Data.Repositories;
using reg.Models.DTOs;

namespace reg.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly UserRepository _userRepository;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _userManager;

        public RoleController(
            UserRepository userRepository,
            RoleManager<IdentityRole> roleManager,
            UserManager<User> userManager)
        {
            _userRepository = userRepository;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = _roleManager.Roles.ToList();
            return Ok(roles);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return BadRequest("Имя роли не может быть пустым");

            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (roleExists)
                return BadRequest($"Роль {roleName} уже существует");

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

            if (result.Succeeded)
                return Ok($"Роль {roleName} успешно создана");

            return BadRequest($"Не удалось создать роль: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignRoleToUser(UserRoleDto userRoleDto)
        {
            // var user = await _userRepository.GetUserByEmail(userRoleDto.Email);
            var user = await _userManager.FindByEmailAsync(userRoleDto.Email);
            if (user == null)
                return NotFound($"Пользователь с email {userRoleDto.Email} не найден");

            var roleExists = await _roleManager.RoleExistsAsync(userRoleDto.Role);
            if (!roleExists)
                return NotFound($"Роль {userRoleDto.Role} не найдена");

            var result = await _userManager.AddToRoleAsync(user, userRoleDto.Role);

            if (result.Succeeded)
                return Ok($"Пользователю {userRoleDto.Email} успешно назначена роль {userRoleDto.Role}");

            return BadRequest($"Не удалось назначить роль: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemoveRoleFromUser(UserRoleDto userRoleDto)
        {
            var user = await _userRepository.GetUserByEmail(userRoleDto.Email);
            if (user == null)
                return NotFound($"Пользователь с email {userRoleDto.Email} не найден");

            var isInRole = await _userManager.IsInRoleAsync(user, userRoleDto.Role);
            if (!isInRole)
                return BadRequest($"Пользователь {userRoleDto.Email} не имеет роли {userRoleDto.Role}");

            var result = await _userManager.RemoveFromRoleAsync(user, userRoleDto.Role);

            if (result.Succeeded)
                return Ok($"Роль {userRoleDto.Role} удалена у пользователя {userRoleDto.Email}");

            return BadRequest($"Не удалось удалить роль: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        [HttpGet("getUserRoles")]
        public async Task<IActionResult> GetUserRoles(string email)
        {
            var user = await _userRepository.GetUserByEmail(email);
            if (user == null)
                return NotFound($"Пользователь с email {email} не найден");

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(roles);
        }
    }
}