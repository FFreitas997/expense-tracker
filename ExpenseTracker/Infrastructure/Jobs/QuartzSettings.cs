namespace Infrastructure.Jobs;

public sealed class QuartzSettings
{
    public string InstanceName { get; set; } = "ExpenseTrackerScheduler";
    public int MaxConcurrency { get; set; } = 5;
    public bool WaitForJobs { get; set; } = true;
    public bool UsePersistentStore { get; set; } = false; // true for production
}