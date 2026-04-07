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
using System.Diagnostics;
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
            var sw = Stopwatch.StartNew();
            var fromEmail = message.From ?? _opt.DefaultFrom;
            var fromName = message.FromName ?? _opt.DefaultFromName;

            _logger.LogInformation(
                "[EMAIL-SMTP] Start sending email | Host={Host}:{Port} | From={From} | To={To} | Subject={Subject}",
                _opt.Host,
                _opt.Port,
                fromEmail,
                message.To,
                message.Subject);

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

                _logger.LogInformation(
                    "[EMAIL-SMTP] Send success | To={To} | Subject={Subject} | DurationMs={DurationMs}",
                    message.To,
                    message.Subject,
                    sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[EMAIL-SMTP] Send failed | Host={Host}:{Port} | From={From} | To={To} | Subject={Subject} | DurationMs={DurationMs}",
                    _opt.Host,
                    _opt.Port,
                    fromEmail,
                    message.To,
                    message.Subject,
                    sw.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                if (client.IsConnected)
                    await client.DisconnectAsync(true, ct);

                _logger.LogInformation(
                    "[EMAIL-SMTP] End send attempt | To={To} | Subject={Subject} | TotalDurationMs={DurationMs}",
                    message.To,
                    message.Subject,
                    sw.ElapsedMilliseconds);
            }
        }

    }
}
