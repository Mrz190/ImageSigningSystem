using System.Net;
using System.Net.Mail;

namespace API.Services
{
    public class MailService
    {
        private const int MaxRetryAttempts = 3;
        private const int DelayMilliseconds = 2000;
        private readonly IConfiguration _config;

        public MailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<bool> SendMailAsync(MailRequest mailRequest)
        {
            var recipientEmail = mailRequest.RecipientEmail;

            if (string.IsNullOrEmpty(recipientEmail)) return false;

            using var smtp = CreateSmtpClient();

            bool success = await SendWithRetryAsync(smtp, mailRequest);

            return success;
        }

        private SmtpClient CreateSmtpClient()
        {
            return new SmtpClient(_config.GetValue<string>("MailSettings:SmtpServer"), _config.GetValue<int>("MailSettings:SmtpPort"))
            {
                Credentials = new NetworkCredential(_config.GetValue<string>("MailSettings:SmtpUser"), _config.GetValue<string>("MailSettings:SmtpPass")),
                EnableSsl = true
            };
        }

        private async Task<bool> SendWithRetryAsync(SmtpClient smtp, MailRequest mailRequest)
        {
            var mailMessage = CreateMailMessage(mailRequest);

            for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
            {
                try
                {
                    await smtp.SendMailAsync(mailMessage);
                    return true;
                }
                catch (Exception)
                {
                    if (attempt == MaxRetryAttempts - 1)
                    {
                        return false;
                    }

                    await Task.Delay(DelayMilliseconds * (int)Math.Pow(2, attempt));
                }
            }

            return false;
        }

        private MailMessage CreateMailMessage(MailRequest mailRequest)
        {
            if (string.IsNullOrEmpty(_config.GetValue<string>("MailSettings:SmtpUser")))
            {
                throw new ArgumentNullException("SmtpUser is not configured properly in MailSettings.");
            }

            var mailMessage = new MailMessage(_config.GetValue<string>("MailSettings:SmtpUser"), mailRequest.RecipientEmail, mailRequest.MailMessage.Subject, mailRequest.MailMessage.MessageBody)
            {
                Priority = MailPriority.Normal,
                IsBodyHtml = true
            };

            mailMessage.Headers.Add("X-Priority", "3");
            mailMessage.Headers.Add("X-MSMail-Priority", "Normal");
            mailMessage.Headers.Add("Disposition-Notification-To", _config.GetValue<string>("MailSettings:SmtpUser"));

            return mailMessage;
        }
    }

    public class Message
    {
        public string Subject { get; set; } = "Image Signature Status";
        public string MessageBody { get; set; }
    }

    public class MailRequest
    {
        public Message MailMessage { get; set; }
        public string RecipientEmail { get; set; }
    }
}
