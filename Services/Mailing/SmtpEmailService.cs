using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AbsoluteCinema.Configuration;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AbsoluteCinema.Services.Mailing;

public sealed class SmtpEmailService(IOptionsMonitor<SmtpConfiguration> configuration, ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendAsync(IEnumerable<string> recipients, string subject, string body, IEnumerable<string> attachmentPaths, CancellationToken cancellationToken = default)
    {
        var smtp = configuration.CurrentValue;
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));
        message.To.AddRange(recipients.Select(MailboxAddress.Parse));
        message.Subject = subject;

        var builder = new BodyBuilder { TextBody = body };

        foreach (var path in attachmentPaths)
            await builder.Attachments.AddAsync(path, cancellationToken);

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(smtp.Host, smtp.Port, smtp.UseSsl, cancellationToken);

        if (!string.IsNullOrEmpty(smtp.Username))
            await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("Email sent to {Count} recipients with subject '{Subject}'", message.To.Count, subject);
    }
}
