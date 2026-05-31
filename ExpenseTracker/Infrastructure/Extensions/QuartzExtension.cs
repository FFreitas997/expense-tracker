using Infrastructure.Jobs.Setup;
using Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Infrastructure.Extensions;

internal static class QuartzExtension
{
    internal static IServiceCollection AddQuartzJobs(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("Quartz").Get<QuartzSettings>();

        if (settings is null)
            throw new InvalidOperationException("Quartz settings are not configured.");

        services.Configure<QuartzSettings>(configuration.GetSection(nameof(QuartzSettings)));

        services.ConfigureOptions<QuartzJobSetup>();

        services.AddQuartz(q =>
        {
            q.SchedulerName = settings.InstanceName;
            q.UseDefaultThreadPool(tp => tp.MaxConcurrency = settings.MaxConcurrency);
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = settings.WaitForJobs;
            options.AwaitApplicationStarted = true;
        });

        return services;
    }
}