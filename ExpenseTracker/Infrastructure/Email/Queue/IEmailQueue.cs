using Infrastructure.Email.Models;

namespace Infrastructure.Email.Queue;

public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct = default);
}