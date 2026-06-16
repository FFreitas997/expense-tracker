using Infrastructure.Email.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Email.Service;

/// <summary>
/// Sends email messages over SMTP using MailKit.
/// Configured via <see cref="EmailSettings"/> (host, port, credentials, TLS).
/// </summary>
public sealed class SmtpEmailService(
    IOptions<EmailSettings> options,
    ILogger<SmtpEmailService> logger
) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    /// <summary>
    /// Builds a MIME message from <paramref name="message"/>, opens a fresh SMTP
    /// connection, authenticates, sends, and disconnects gracefully.
    /// </summary>
    /// <param name="message">The email to send, including recipient, subject, and body.</param>
    /// <param name="ct">Token used to cancel the SMTP operations.</param>
    /// <exception cref="Exception">
    /// Any SMTP or network exception is logged and re-thrown so the caller
    /// (e.g. <c>EmailSenderService</c>) can decide how to handle failures.
    /// </exception>
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            var email = new MimeMessage();

            // Set the sender display name and address from configuration
            email.From.Add(new MailboxAddress(
                _settings.FromName,
                _settings.FromAddress));

            // Set the recipient display name and address from the message
            email.To.Add(new MailboxAddress(
                message.ToName,
                message.To));

            email.Subject = message.Subject;

            // Use "html" subtype for rich content, "plain" for plain text
            email.Body = new TextPart(message.IsHtml ? "html" : "plain")
            {
                Text = message.Body
            };

            using var client = new SmtpClient();

            // Connect with StartTLS when SSL is required, otherwise use no encryption
            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                _settings.UseSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None,
                ct);

            await client.AuthenticateAsync(
                _settings.Username,
                _settings.Password,
                ct);

            await client.SendAsync(email, ct);

            // Sends QUIT to the server before closing the connection
            await client.DisconnectAsync(quit: true, ct);

            logger.LogInformation(
                "Email sent successfully to {To}. Subject: {Subject}",
                message.To,
                message.Subject);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send email to {To}. Subject: {Subject}",
                message.To,
                message.Subject);

            throw;
        }
    }
}