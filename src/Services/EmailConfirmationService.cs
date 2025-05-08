using reg.Utils;

namespace reg.Services
{
    public class EmailConfirmationService
    {
        private readonly EmailSender _emailSender;
        private readonly string _hostUrl;

        public EmailConfirmationService(EmailSender emailSender, string hostUrl)
        {
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _hostUrl = hostUrl ?? throw new ArgumentNullException(nameof(hostUrl));
        }

        public async Task SendConfirmationEmailAsync(string email, string token)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException("Email не может быть пустым", nameof(email));

            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("Token не может быть пустым", nameof(token));

            string confirmationLink = $"{_hostUrl}/api/auth/confirm-email?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";
            string message = $"Пожалуйста, подтвердите ваш email, перейдя по следующей ссылке: {confirmationLink}";

            await _emailSender.SendEmailAsync(email, "Подтверждение email", message);
        }
    }
}