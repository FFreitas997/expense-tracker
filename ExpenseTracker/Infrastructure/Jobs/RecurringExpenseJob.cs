using System.Data;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.UnitOfWork.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Infrastructure.Jobs;

/// <summary>
/// Quartz.NET background job that processes all recurring expenses that are due and materialises
/// them as real <see cref="Expense"/> entries in the database.
/// <para>
/// Scheduled via <see cref="QuartzJobSetup"/> using the cron expression defined in
/// <see cref="JobKeys.RecurringExpense.CronExpression"/> (every day at midnight UTC).
/// </para>
/// </summary>
/// <remarks>
/// <see cref="DisallowConcurrentExecutionAttribute"/> ensures that a second trigger cannot fire
/// while a previous execution is still running, preventing duplicate expense creation.
/// </remarks>
[DisallowConcurrentExecution]
public sealed class RecurringExpenseJob(IServiceScopeFactory factory, ILogger<RecurringExpenseJob> logger) : IJob
{
    /// <summary>
    /// Entry point called by the Quartz scheduler on each trigger fire.
    /// Resolves a fresh <see cref="IUnitOfWork"/> from a dedicated DI scope, fetches all due
    /// recurring expenses, converts them to real expenses inside a
    /// <see cref="IsolationLevel.RepeatableRead"/> transaction, and advances each template's
    /// next due date.
    /// </summary>
    /// <param name="context">Quartz execution context providing the cancellation token and job metadata.</param>
    /// <exception cref="JobExecutionException">
    /// Wraps any unhandled exception and instructs Quartz not to refire the job immediately.
    /// </exception>
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("RecurringExpenseJob started at {Time}.", DateTimeOffset.UtcNow);

        // Create a dedicated DI scope so that scoped services (e.g. DbContext) are properly
        // isolated from other concurrent requests and disposed when the job finishes.
        await using var scope = factory.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Retrieve all recurring expense templates whose NextDueDate is on or before now.
        var dueRecurring = await unitOfWork.RecurringExpenses
            .GetDueAsync(DateTime.UtcNow, context.CancellationToken);

        if (dueRecurring.Count == 0)
        {
            logger.LogInformation("No recurring expenses due. Skipping.");
            return;
        }

        // Use RepeatableRead isolation to guard against phantom reads while iterating due templates.
        await using var transaction = await unitOfWork
            .BeginTransactionAsync(IsolationLevel.RepeatableRead, context.CancellationToken);

        try
        {
            var createdCount = 0;

            foreach (var recurring in dueRecurring)
            {
                // Materialise the recurring template into a concrete expense entry.
                var expense = MapToExpense(recurring);
                await unitOfWork.Expenses.CreateAsync(expense, context.CancellationToken);

                // Advance the template's next due date so it is not processed again this cycle.
                recurring.NextDueDate = CalculateNextDueDate(recurring.NextDueDate, recurring.Frequency);

                // Stamp the audit fields to indicate this system job performed the update.
                recurring.ModifiedAt = DateTime.UtcNow;
                recurring.ModifiedBy = "RecurringExpenseJob";
                await unitOfWork.RecurringExpenses.UpdateAsync(recurring, context.CancellationToken);

                createdCount++;
            }

            // Flush all tracked changes to the database within the open transaction.
            await unitOfWork.SaveChangesAsync(context.CancellationToken);

            // Commit only after every expense and template update has been written successfully.
            await transaction.CommitAsync(context.CancellationToken);

            logger.LogInformation("RecurringExpenseJob completed. Created {Count} expense(s).", createdCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RecurringExpenseJob failed at {Time}.", DateTimeOffset.UtcNow);

            // Roll back all changes made during this execution to maintain data consistency.
            await transaction.RollbackAsync(context.CancellationToken);

            // Wrap in JobExecutionException with refireImmediately: false so Quartz does not
            // immediately reschedule the job, avoiding a tight error loop.
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }

    // ── Helpers ───────────────────────────────────────────────

    /// <summary>
    /// Maps a <see cref="RecurringExpense"/> template to a new concrete <see cref="Expense"/> entity,
    /// preserving amount, currency, category, and user ownership.
    /// </summary>
    /// <param name="recurring">The recurring template to materialise.</param>
    /// <returns>A fully initialised <see cref="Expense"/> ready to be persisted.</returns>
    private static Expense MapToExpense(RecurringExpense recurring)
    {
        var now = DateTime.UtcNow;
        return new Expense
        {
            Id = Guid.NewGuid(),
            Amount = recurring.Amount,
            Currency = recurring.Currency,
            Description = $"[Recurring] {recurring.Description}",
            Date = now,
            PaymentMethod = PaymentMethod.Other,

            UserId = recurring.UserId,
            CategoryId = recurring.CategoryId,

            CreatedAt = now,
            CreatedBy = "RecurringExpenseJob"
        };
    }

    /// <summary>
    /// Calculates the next due date for a recurring expense by advancing <paramref name="current"/>
    /// by one period according to <paramref name="frequency"/>.
    /// </summary>
    /// <param name="current">The current due date to advance from.</param>
    /// <param name="frequency">The recurrence frequency that determines the period length.</param>
    /// <returns>The next due date after the given <paramref name="current"/> date.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="frequency"/> does not match any supported <see cref="RecurringFrequency"/> value.
    /// </exception>
    private static DateTime CalculateNextDueDate(DateTime current, RecurringFrequency frequency)
    {
        return frequency switch
        {
            RecurringFrequency.Daily   => current.AddDays(1),
            RecurringFrequency.Weekly  => current.AddDays(7),
            RecurringFrequency.Monthly => current.AddMonths(1),
            RecurringFrequency.Yearly  => current.AddYears(1),
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Unsupported frequency.")
        };
    }
}