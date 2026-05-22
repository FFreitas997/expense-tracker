using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Settings;

public class DatabaseSettings
{
    [Required, MinLength(1)]
    public string ConnectionString { get; set; } = string.Empty;

    [Range(1, 10)]
    public int MaxRetryCount { get; set; } = 3;

    [Range(1, 300)]
    public int CommandTimeout { get; set; } = 30;

    public bool EnableSensitiveDataLogging { get; set; }

    public bool EnableDetailedErrors { get; set; }
}