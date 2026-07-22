using InnNou.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace InnNou.Infrastructure.Services
{
    // SMTP-relay-backed IEmailSender implementation. Real credentials come from user-secrets in
    // local dev (Smtp:Username/Password/FromAddress) — appsettings.json only carries the
    // non-secret Host/Port, same "local-dev placeholder, real secret comes from elsewhere"
    // convention as the JWT signing key.
    public class SmtpEmailSender(IConfiguration configuration) : IEmailSender
    {
        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            var fromAddress = configuration["Smtp:FromAddress"]
                ?? throw new InvalidOperationException("Smtp:FromAddress is not configured.");

            using var mail = new MailMessage
            {
                From = new MailAddress(fromAddress),
                Subject = message.Subject,
                Body = message.HtmlBody,
                IsBodyHtml = true
            };
            mail.To.Add(message.ToAddress);

            foreach (var attachment in message.Attachments)
                mail.Attachments.Add(new Attachment(new MemoryStream(attachment.Content), attachment.FileName, attachment.ContentType));

            using var smtp = new SmtpClient
            {
                Host = configuration["Smtp:Host"] ?? "smtp-relay.brevo.com",
                Port = int.TryParse(configuration["Smtp:Port"], out var port) ? port : 587,
                Credentials = new NetworkCredential(configuration["Smtp:Username"], configuration["Smtp:Password"]),
                EnableSsl = true
            };

            await smtp.SendMailAsync(mail, cancellationToken);
        }
    }
}
