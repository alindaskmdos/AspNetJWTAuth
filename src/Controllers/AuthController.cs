using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using reg.Models;
using reg.Data.Repositories;
using reg.Models.DTOs;
using reg.Services;
using reg.Utils;

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
        private readonly EmailSender _emailSender;

        public AuthController(
            UserRepository userRepository,
            TokenService tokenService,
            TokenRepository tokenRepository,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            EmailSender emailSender)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _tokenRepository = tokenRepository;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
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

            if (!await _userManager.GetTwoFactorEnabledAsync(user))
                await _userManager.SetTwoFactorEnabledAsync(user, true);

            var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
            await _emailSender.SendEmailAsync(user.Email ?? string.Empty, "Код подтверждения", $"Ваш код: {code}");

            return Ok(new { requires2FA = true });
        }

        [HttpPost("2fa-login")]
        public async Task<IActionResult> TwoFactorLogin(Auth2FaDto auth2FaDto)
        {
            User? user = await _userRepository.GetUserByEmail(auth2FaDto.Email);
            if (user == null)
                return Unauthorized("Пользователь с таким email не найден");

            var valid = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", auth2FaDto.Code);
            if (!valid)
                return Unauthorized("Код неверный");

            string accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            string refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
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

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshTokens(RefreshTokenDto refreshTokenDto)
        {
            try
            {
                string IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

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

        // подключить IDistributedCache для блэклиста jwt (до истечения их срока) для полного контроля logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenDto refreshTokenDto)
        {
            string IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

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

