using System.ComponentModel.DataAnnotations;

namespace API.Security.RateLimiting;

public class RateLimiterSettings
{
    // Enable or disable rate limiting
    public bool Enabled { get; set; } = true;

    // Maximum number of requests allowed in the time window
    [Range(1, int.MaxValue)] public int PermitLimit { get; set; } = 100;

    // Time window in minutes for rate limiting
    [Range(1, int.MaxValue)] public int WindowMinutes { get; set; } = 1;

    // Maximum number of requests allowed in the queue
    [Range(0, int.MaxValue)] public int QueueLimit { get; set; } = 0;

    // Number of seconds to wait before retrying after being rate limited
    [Range(1, int.MaxValue)] public int RetryAfterSeconds { get; set; } = 60;
}