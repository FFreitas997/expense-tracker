using Quartz;

namespace Infrastructure.Jobs.Keys;

public static class JobKeys
{
    public static class RecurringExpense
    {
        public const string CronExpression = "0 0 0 * * ?"; // every day at midnight
        public static readonly JobKey Job = new("recurring-expense-job", "expense");
        public static readonly TriggerKey Trigger = new("recurring-expense-trigger", "expense");
    }
}