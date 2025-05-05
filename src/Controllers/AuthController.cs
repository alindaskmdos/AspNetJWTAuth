using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using reg.Models;
using reg.Data.Repositories;
using reg.Models.DTOs;
using reg.Services;

namespace reg.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserRepository _userRepository;
        private readonly TokenService _tokenService;
        private readonly TokenRepository _tokenRepository;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AuthController(
            UserRepository userRepository,
            TokenService tokenService,
            TokenRepository tokenRepository,
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _tokenRepository = tokenRepository;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            User? user = await _userRepository.GetUserByEmail(loginDto.Email);
            if (user == null)
                return BadRequest("Пользователь с таким email не найден");

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded)
                return BadRequest("Неверный пароль");

            string accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            string refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                CreatedAt = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _tokenRepository.AddRefreshTokenAsync(refreshTokenEntity);

            var response = new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddHours(1)
            };

            return Ok(response);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshTokens(RefreshTokenDto refreshTokenDto)
        {
            try
            {
                string IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
                string IpAddressFromToken = await _tokenRepository.getIpAddressByRefreshToken(refreshTokenDto.RefreshToken);

                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(refreshTokenDto.AccessToken);
                var email = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

                if (string.IsNullOrEmpty(email))
                    return BadRequest("Email пользователя не найден в токене");

                var (accessToken, refreshToken) = await _tokenService.RefreshAccessTokenAsync(
                    email, refreshTokenDto.RefreshToken);

                var response = new TokenResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    Expiration = DateTime.UtcNow.AddHours(1)
                };

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(CreateUserDto userDto)
        {
            try
            {
                User? newUser = await _userRepository.RegisterUserAsync(userDto);

                if (newUser == null)
                    return BadRequest("Ошибка регистрации пользователя");

                string accessToken = await _tokenService.GenerateAccessTokenAsync(newUser);
                string refreshToken = await _tokenService.GenerateRefreshTokenAsync(newUser);

                var refreshTokenEntity = new RefreshToken
                {
                    UserId = newUser.Id,
                    Token = refreshToken,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };

                await _tokenRepository.AddRefreshTokenAsync(refreshTokenEntity);

                var response = new TokenResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    Expiration = DateTime.UtcNow.AddHours(1)
                };

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // подключить редис для блэклиста jwt (до истечения их срока) для полного контроля logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenDto refreshTokenDto)
        {
            string IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            string IpAddressFromToken = await _tokenRepository.getIpAddressByRefreshToken(refreshTokenDto.RefreshToken);

            if (IpAddress != IpAddressFromToken)
                return BadRequest("IP адрес не совпадает с тем, что был использован при входе");

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(refreshTokenDto.AccessToken);
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
                return BadRequest("Email пользователя не найден в токене");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return NotFound($"Пользователь с email {email} не найден");

            await _tokenRepository.RevokeRefreshTokenAsync(user.Id, refreshTokenDto.RefreshToken);
            await _signInManager.SignOutAsync();

            return Ok("Выход выполнен успешно");
        }
    }
}

