using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Identity;

internal static class IdentityExtension
{
    internal static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var settings = configuration.GetSection("Identity").Get<IdentitySettings>();

        if (settings is null)
            throw new InvalidOperationException("Identity settings are not configured properly.");

        services
            .AddOptions<IdentitySettings>()
            .Bind(configuration.GetSection("Identity"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddIdentity<User, IdentityRole<Guid>>(options =>
            {
                // Password
                options.Password.RequiredLength = settings.Password.PasswordMinLength;
                options.Password.RequireDigit = settings.Password.RequireDigit;
                options.Password.RequireUppercase = settings.Password.RequireUppercase;
                options.Password.RequireLowercase = settings.Password.RequireLowercase;
                options.Password.RequireNonAlphanumeric = settings.Password.RequireNonAlphanumeric;
                options.Password.RequiredUniqueChars = settings.Password.RequiredUniqueChars;

                // Lockout
                options.Lockout.AllowedForNewUsers = settings.Lockout.AllowedForNewUsers;
                options.Lockout.MaxFailedAccessAttempts = settings.Lockout.MaxFailedAccessAttempts;
                options.Lockout.DefaultLockoutTimeSpan = settings.Lockout.DefaultLockoutTimeSpan;

                // User — restrict username to email-safe characters
                options.User.RequireUniqueEmail = settings.User.RequireUniqueEmail;
                options.User.AllowedUserNameCharacters = settings.User.AllowedUserNameCharacters;

                // Disabled until email delivery is configured;
                // enable RequireConfirmedEmail and RequireConfirmedAccount together when ready
                options.SignIn.RequireConfirmedEmail = settings.SignIn.RequireConfirmedEmail;
                options.SignIn.RequireConfirmedAccount = settings.SignIn.RequireConfirmedAccount;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<DataProtectorTokenProvider<User>>(TokenOptions.DefaultProvider);

        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromHours(settings.Token.LifespanHours));

        return services;
    }
}