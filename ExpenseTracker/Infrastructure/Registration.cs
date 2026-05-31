using Infrastructure.Cache.Implementations;
using Infrastructure.Cache.Interfaces;
using Infrastructure.Extensions;
using Infrastructure.Repositories.Implementations.Resources;
using Infrastructure.Repositories.Interfaces.Resources;
using Infrastructure.Seeds;
using Infrastructure.Seeds.Seeders;
using Infrastructure.UnitOfWork.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure;

public static class Registration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // ── Database Context ─────────────────────────────────────
        services.AddDatabaseContext(configuration);

        // ── Caching ─────────────────────────────────────────────
        services.AddInMemoryCache(configuration, environment);

        // ── Identity ────────────────────────────────────────────
        services.AddIdentityServices(configuration);

        // ── Caching Repository ─────────────────────────────────────────
        services.AddSingleton<ICacheRepository, InMemoryCacheRepository>();

        // ── Repositories ────────────────────────────────────────────────
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IRecurringExpenseRepository, RecurringExpenseRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork.Implementations.UnitOfWork>();
        services.AddScoped<IAppDbContext, AppDbContext>();

        // ── Seeders ────────────────────────────────────────────
        services.AddScoped<RoleSeeder>();
        services.AddScoped<DefaultCategorySeeder>();
        services.AddScoped<DatabaseSeeder>();

        // ── Quartz Jobs ───────────────────────────────────────────────
        services.AddQuartzJobs(configuration);

        return services;
    }
}