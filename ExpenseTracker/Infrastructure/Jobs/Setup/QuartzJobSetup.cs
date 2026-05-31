using Infrastructure.Jobs.Keys;
using Microsoft.Extensions.Options;
using Quartz;

namespace Infrastructure.Jobs.Setup;

public sealed class QuartzJobSetup : IConfigureOptions<QuartzOptions>
{
    public void Configure(QuartzOptions options)
    {
        // ── RecurringExpenseJob ──────────────────────────────
        options.AddJob<RecurringExpenseJob>(builder => builder
            .WithIdentity(JobKeys.RecurringExpense.Job)
            .WithDescription("Processes due recurring expenses and creates expense entries.")
            .StoreDurably());

        options.AddTrigger(builder => builder
            .ForJob(JobKeys.RecurringExpense.Job)
            .WithIdentity(JobKeys.RecurringExpense.Trigger)
            .WithDescription("Fires every day at midnight.")
            .WithCronSchedule(JobKeys.RecurringExpense.CronExpression, cron => cron.InTimeZone(TimeZoneInfo.Utc))
            .StartNow());
    }
}