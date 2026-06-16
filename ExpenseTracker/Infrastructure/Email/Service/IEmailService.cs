using Infrastructure.Email.Models;

namespace Infrastructure.Email.Service;

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}