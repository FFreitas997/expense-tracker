using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Settings;

public class IdentitySettings
{
    [Required]
    public required PasswordSettings Password { get; set; }

    [Required]
    public required LockoutSettings Lockout { get; set; }

    [Required]
    public required UserSettings User { get; set; }

    [Required]
    public required SignInSettings SignIn { get; set; }

    [Required]
    public required TokenSettings Token { get; set; }
}

public class PasswordSettings
{
    [Range(8, 128)]
    public int PasswordMinLength { get; set; } = 12;

    public bool RequireDigit { get; set; } = true;

    public bool RequireUppercase { get; set; } = true;

    public bool RequireLowercase { get; set; } = true;

    public bool RequireNonAlphanumeric { get; set; } = true;

    [Range(1, 128)]
    public int RequiredUniqueChars { get; set; } = 6;
}

public class LockoutSettings
{
    public bool AllowedForNewUsers { get; set; } = true;

    [Range(1, 100)]
    public int MaxFailedAccessAttempts { get; set; } = 5;

    public TimeSpan DefaultLockoutTimeSpan { get; set; } = TimeSpan.FromMinutes(30);
}

public class UserSettings
{
    public bool RequireUniqueEmail { get; set; } = true;

    [Required, MinLength(1)]
    public string AllowedUserNameCharacters { get; set; } =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789@.-_+";
}

public class SignInSettings
{
    public bool RequireConfirmedEmail { get; set; } = false;

    public bool RequireConfirmedAccount { get; set; } = false;
}

public class TokenSettings
{
    [Range(1, 24)]
    public int LifespanHours { get; set; } = 2;
}