using System.Net.Mail;
using System.Net.Sockets;
using DnsClient;

namespace reg.Utils
{
    public class EmailSmtpChecker
    {
        private readonly ILogger<EmailSmtpChecker> _logger;
        private const string MY_DOMAIN = "mycompany.com";
        private const int SMTP_PORT = 25;
        private const int CONNECTION_TIMEOUT_SEC = 10;
        private const string VERIFICATION_EMAIL = "verification@" + MY_DOMAIN;

        public EmailSmtpChecker(ILogger<EmailSmtpChecker> logger)
        {
            _logger = logger;
        }

        public async Task<bool> VerifyEmailAsync(string email)
        {
            return true;

            // _logger.LogInformation("Проверка email: {Email}", email);
            // if (!IsValidEmailSyntax(email))
            // {
            //     _logger.LogWarning("Некорректный синтаксис email: {Email}", email);
            //     return false;
            // }
            // string domain = email.Split('@')[1];

            // var mxRecords = GetMxRecords(domain);
            // if (mxRecords.Count == 0)
            // {
            //     _logger.LogWarning("Не найдены MX-записи для домена: {Domain}", domain);
            //     return false;
            // }

            // foreach (var mx in mxRecords)
            // {
            //     _logger.LogInformation("Пробуем MX-сервер: {Mx}", mx);
            //     if (await CheckEmailViaSmtpAsync(email, mx))
            //     {
            //         _logger.LogInformation("Email подтверждён через MX: {Mx}", mx);
            //         return true;
            //     }
            // }
            // _logger.LogWarning("Email не подтверждён ни одним MX-сервером: {Email}", email);
            // return false;
        }

        private static bool IsValidEmailSyntax(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private List<string> GetMxRecords(string domain)
        {
            if (domain == "test.local")
                return new List<string> { "127.0.0.1" };

            try
            {
                var lookup = new LookupClient();
                var result = lookup.Query(domain, QueryType.MX);
                var mxs = result.Answers.MxRecords()
                    .OrderBy(x => x.Preference)
                    .Select(x => x.Exchange.Value)
                    .ToList();
                _logger.LogInformation("MX-записи для {Domain}: {MxList}", domain, string.Join(", ", mxs));
                return mxs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении MX-записей для {Domain}", domain);
                return new List<string>();
            }
        }

        private async Task<bool> CheckEmailViaSmtpAsync(string email, string mxServer)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(mxServer, SMTP_PORT);
                if (await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(CONNECTION_TIMEOUT_SEC))) != connectTask)
                {
                    _logger.LogWarning("Таймаут подключения к SMTP: {MxServer}", mxServer);
                    return false;
                }

                using var stream = client.GetStream();
                stream.ReadTimeout = stream.WriteTimeout = CONNECTION_TIMEOUT_SEC * 1000;
                using var reader = new StreamReader(stream);
                using var writer = new StreamWriter(stream) { AutoFlush = true };

                var greeting = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(greeting) || !greeting.StartsWith("220"))
                {
                    _logger.LogWarning("Некорректный SMTP greeting от {MxServer}: {Greeting}", mxServer, greeting);
                    return false;
                }

                await writer.WriteLineAsync($"HELO {MY_DOMAIN}");
                var response = await reader.ReadLineAsync();
                if (response == null || !response.StartsWith("250"))
                {
                    _logger.LogWarning("HELO не принят сервером {MxServer}: {Response}", mxServer, response);
                    return false;
                }

                await writer.WriteLineAsync($"MAIL FROM:<{VERIFICATION_EMAIL}>");
                response = await reader.ReadLineAsync();
                if (response == null || !response.StartsWith("250"))
                {
                    _logger.LogWarning("MAIL FROM не принят сервером {MxServer}: {Response}", mxServer, response);
                    return false;
                }

                await writer.WriteLineAsync($"RCPT TO:<{email}>");
                response = await reader.ReadLineAsync();
                bool result = response != null && response.StartsWith("250");
                _logger.LogInformation("RCPT TO ответ от {MxServer}: {Response}, результат: {Result}", mxServer, response, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка SMTP для {MxServer}", mxServer);
                return false;
            }
        }
    }
}
