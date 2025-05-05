using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using reg.Models;
using reg.Models.DTOs;
using reg.Utils;

namespace reg.Data.Repositories
{
    public class UserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly EmailSmtpChecker _emailSmtpChecker;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(
            ApplicationDbContext context,
            UserManager<User> userManager,
            ILogger<UserRepository> logger,
            EmailSmtpChecker emailSmtpChecker)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailSmtpChecker = emailSmtpChecker ?? throw new ArgumentNullException(nameof(emailSmtpChecker));
        }

        public async Task<User?> RegisterUserAsync(CreateUserDto createUserDto)
        {
            if (createUserDto == null)
            {
                _logger.LogError("CreateUserDto is null");
                throw new ArgumentNullException(nameof(createUserDto));
            }

            if (string.IsNullOrEmpty(createUserDto.Email))
            {
                _logger.LogError("Email is null or empty");
                throw new ArgumentException("Email не может быть пустым", nameof(createUserDto.Email));
            }

            if (string.IsNullOrEmpty(createUserDto.Password))
            {
                _logger.LogError("Password is null or empty");
                throw new ArgumentException("Пароль не может быть пустым", nameof(createUserDto.Password));
            }

            bool checkEmail = await _emailSmtpChecker.VerifyEmailAsync(createUserDto.Email);

            if (checkEmail == false)
            {
                _logger.LogError("Email verification failed for {Email}", createUserDto.Email);
                throw new InvalidOperationException("Ошибка валидации email. Проверьте правильность введенного адреса электронной почты.");
            }

            _logger.LogInformation("Создание пользователя с email: {Email}", createUserDto.Email);

            User user = new User
            {
                UserName = createUserDto.Email,
                Email = createUserDto.Email,
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogDebug("Вызов UserManager.CreateAsync");
            var result = await _userManager.CreateAsync(user, createUserDto.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("Пользователь успешно создан с Id: {UserId}", user.Id);
                return user;
            }

            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Ошибка при создании пользователя: {Errors}", errors);
            throw new InvalidOperationException($"Ошибка регистрации: {errors}");
        }

        public async Task<List<User>> GetAllUsers()
        {
            return await _userManager.Users.ToListAsync();
        }

        public async Task<User?> GetUserByEmail(string? email)
        {
            if (email == null)
                throw new ArgumentNullException(nameof(email));

            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<User?> GetUserById(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));

            return await _userManager.FindByIdAsync(userId);
        }

        // Update
        public async Task<User> UpdateUserProfile(string userId, UpdateUserProfileDto profileDto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"Пользователь с ID {userId} не найден");

            user.FirstName = profileDto.FirstName;
            user.LastName = profileDto.LastName;
            // Можно добавить другие поля профиля

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Ошибка обновления профиля: {errors}");
            }

            return user;
        }

        public async Task<User> ChangeUserEmail(string currentEmail, string newEmail)
        {
            var user = await GetUserByEmail(currentEmail);
            if (user == null)
                throw new KeyNotFoundException($"Пользователь с email {currentEmail} не найден");

            var emailExists = await GetUserByEmail(newEmail);
            if (emailExists != null)
                throw new InvalidOperationException($"Email {newEmail} уже используется другим пользователем");

            user.UserName = newEmail;
            user.Email = newEmail;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Ошибка при изменении email: {errors}");
            }

            return user;
        }

        public async Task<User> ChangeUserPassword(ChangePasswordDto dto)
        {
            var user = await GetUserByEmail(dto.Email);
            if (user == null)
                throw new KeyNotFoundException($"Пользователь с email {dto.Email} не найден");

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Ошибка при смене пароля: {errors}");
            }

            return user;
        }

        // Delete
        public async Task<bool> DeleteUser(string email)
        {
            var user = await GetUserByEmail(email);
            if (user == null)
                throw new KeyNotFoundException($"Пользователь с email {email} не найден");

            var result = await _userManager.DeleteAsync(user);

            return result.Succeeded;
        }

        public async Task<bool> DeleteUserById(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"Пользователь с ID {userId} не найден");

            var result = await _userManager.DeleteAsync(user);

            return result.Succeeded;
        }

        // Additional methods for user management
        public async Task<int> GetTotalUsersCount()
        {
            return await _userManager.Users.CountAsync();
        }

        public async Task<bool> CheckEmailExists(string email)
        {
            return await _userManager.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> LockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"Пользователь с ID {userId} не найден");

            var result = await _userManager.SetLockoutEndDateAsync(user, DateTime.UtcNow.AddYears(100));
            return result.Succeeded;
        }

        public async Task<bool> UnlockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"Пользователь с ID {userId} не найден");

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            return result.Succeeded;
        }
    }
}