using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Infrastructure.Email;

public sealed class EmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken
    )
    {
        if (
            string.IsNullOrWhiteSpace(_options.Host)
            || string.IsNullOrWhiteSpace(_options.FromAddress)
        )
            throw new InvalidOperationException(
                "SMTP host and sender address must be configured before sending email."
            );
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
        };
        message.To.Add(new MailAddress(recipient));
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
        };
        if (!string.IsNullOrWhiteSpace(_options.UserName))
            client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
        await client.SendMailAsync(message, cancellationToken);
    }
}
