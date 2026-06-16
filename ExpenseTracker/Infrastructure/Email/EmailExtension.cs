using Infrastructure.Email.Queue;
using Infrastructure.Email.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Email;

public static class EmailExtension
{
    internal static IServiceCollection AddEmailService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("Email"));

        // Strategy — swap this for SendGridEmailService when needed
        services.AddTransient<IEmailService, SmtpEmailService>();

        // Queue — singleton so the channel lives for the app lifetime
        services.AddSingleton<EmailQueue>();
        services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<EmailQueue>());

        // Consumer — runs as a hosted background service
        services.AddHostedService<EmailSenderService>();

        return services;
    }
}