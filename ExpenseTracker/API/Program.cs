using API.Extensions;
using Application;
using Infrastructure;
using Infrastructure.Seeds;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Enforce TLS 1.2+ on Kestrel and disable the Server response header
builder.AddKestrelTlsConfiguration();

// Add controllers
builder.Services.AddControllers();

// Add API versioning
builder.Services.AddApiVersioningConfiguration();

// Add CORS configuration
builder.Services.AddCorsConfiguration(builder.Environment, builder.Configuration);

// Add rate limiting configuration
builder.Services.AddRateLimitingConfiguration(builder.Environment, builder.Configuration);

// Add HSTS configuration (no-op in Development)
builder.Services.AddHstsConfiguration(builder.Environment);

// Add application services
builder.Services.AddApplication();

// Add infrastructure services
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// Add exception handling services and configure ProblemDetails
builder.Services.AddExceptionHandling();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Add global exception handling middleware
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

// Send HSTS header to instruct browsers to always use HTTPS (skipped in Development)
if (!app.Environment.IsDevelopment()) app.UseHsts();

app.UseHttpsRedirection();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Seed the database with initial data
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.UseCors(CorsExtension.CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.Run();