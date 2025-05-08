using System.Net;
using System.Net.Mail;

namespace reg.Utils
{
    public class EmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration) => _configuration = configuration;

        public async Task SendEmailAsync(string to, string subject, string message)
        {
            var smtp = _configuration.GetSection("SmtpSettings");
            using var client = new SmtpClient(smtp["Host"], int.Parse(smtp["Port"]))
            {
                Credentials = new NetworkCredential(smtp["User"], smtp["Pass"]),
                EnableSsl = true
            };

            var msg = new MailMessage(from: smtp["From"], to: to, subject: subject, body: message) { IsBodyHtml = true };

            await client.SendMailAsync(msg);
        }
    }
}