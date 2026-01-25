using Core.DTO.Email;
using Core.Interface.Service.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Infa.Email
{
    public sealed class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _opt;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<SmtpOptions> opt, ILogger<SmtpEmailSender> logger)
        {
            _opt = opt.Value;
            _logger = logger;
        }

        public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
        {
            var fromEmail = message.From ?? _opt.DefaultFrom;
            var fromName = message.FromName ?? _opt.DefaultFromName;

            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(fromName, fromEmail));
            mime.To.Add(MailboxAddress.Parse(message.To));
            mime.Subject = message.Subject;

            mime.Body = new BodyBuilder { HtmlBody = message.HtmlBody }.ToMessageBody();

            using var client = new SmtpClient();

            try
            {
                // avoid XOAUTH2 unless you explicitly need it
                client.AuthenticationMechanisms.Remove("XOAUTH2");

                var secure = _opt.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

                await client.ConnectAsync(_opt.Host, _opt.Port, secure, ct);

                // authenticate only when configured
                if (!string.IsNullOrWhiteSpace(_opt.Username))
                    await client.AuthenticateAsync(_opt.Username, _opt.Password, ct);

                await client.SendAsync(mime, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP send failed to {To} (Subject: {Subject})", message.To, message.Subject);
                throw;
            }
            finally
            {
                if (client.IsConnected)
                    await client.DisconnectAsync(true, ct);
            }
        }

    }
}
