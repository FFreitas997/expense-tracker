using Infrastructure.Email.Models;
using Infrastructure.Email.Queue;
using Infrastructure.Email.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Email;

/// <summary>
/// A <see cref="BackgroundService"/> that continuously drains the <see cref="EmailQueue"/>
/// and dispatches each <see cref="EmailMessage"/> via <see cref="IEmailService"/>.
/// A new DI scope is created per message so that scoped services (e.g. database contexts)
/// are resolved and disposed correctly for every send operation.
/// </summary>
public sealed class EmailSenderService(
    EmailQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<EmailSenderService> logger)
    : BackgroundService
{
    /// <summary>
    /// Entry point called by the host. Iterates over all messages produced to
    /// <see cref="EmailQueue.Reader"/> until the <paramref name="ct"/> is cancelled
    /// (i.e. the application is shutting down).
    /// </summary>
    /// <param name="ct">Triggered by the host when the application is stopping.</param>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("EmailSenderService started.");

        await foreach (var message in queue.Reader.ReadAllAsync(ct)) await ProcessAsync(message, ct);
    }

    /// <summary>
    /// Resolves <see cref="IEmailService"/> from a fresh async scope and sends
    /// the message. Exceptions are caught and logged so that a single failure
    /// does not terminate the background loop.
    /// </summary>
    /// <param name="message">The email message dequeued from <see cref="EmailQueue"/>.</param>
    /// <param name="ct">Propagated from <see cref="ExecuteAsync"/> to support cancellation.</param>
    private async Task ProcessAsync(EmailMessage message, CancellationToken ct)
    {
        try
        {
            // Create a fresh scope per message to avoid capturing long-lived scoped services
            await using var scope = scopeFactory.CreateAsyncScope();

            var emailService = scope.ServiceProvider
                .GetRequiredService<IEmailService>();

            await emailService.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            // Log and continue — don't crash the background service
            logger.LogError(
                ex,
                "Error processing email to {To}. Subject: {Subject}",
                message.To,
                message.Subject);
        }
    }

    /// <summary>
    /// Called by the host during graceful shutdown. Logs the stop event and
    /// delegates to the base implementation which cancels the execution token.
    /// </summary>
    /// <param name="ct">Timeout token provided by the host for the shutdown window.</param>
    public override async Task StopAsync(CancellationToken ct)
    {
        logger.LogInformation("EmailSenderService stopping.");
        await base.StopAsync(ct);
    }
}