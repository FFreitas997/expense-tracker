using API.Exceptions;
using API.Extensions;
using API.Filters;
using API.Middlewares;
using API.Observability.Logging;
using API.Observability.Tracing;
using API.Security.Authentication;
using API.Security.Authorization;
using API.Security.Cors;
using API.Security.RateLimiting;
using Application;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Seeds;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog — must be first ──────────────────────────────────
// Initialise structured logging before any other registration so that
// all subsequent startup activity is captured in the log output.
builder.AddStructuredLogging();

// ── Kestrel ───────────────────────────────────────────────────
// Enforce TLS 1.2+ on Kestrel and suppress the Server response header
// to reduce information exposure in HTTP responses.
builder.AddKestrelTlsConfiguration();

// ── Controllers ───────────────────────────────────────────────
builder.Services.AddControllers(options =>
{
    // Attach a global filter so every action validates its input model
    // via FluentValidation before the action body executes.
    options.Filters.Add<ValidationFilter>();
});

// ── API versioning ────────────────────────────────────────────
// Enables URL-segment versioning (e.g. /api/v1/...) with a default
// version so unversioned clients are handled gracefully.
builder.Services.AddApiVersioningConfiguration();

// ── Authentication ───────────────────────────
// Registers JWT bearer authentication and configures token validation
builder.Services.AddJwtAuthentication(builder.Configuration);

// ── Authorization ─────────────────────────────────────────────
// Registers authorization policies and handlers for role-based and
builder.Services.AddAppAuthorization();

// ── CORS ──────────────────────────────────────────────────────
// Configures allowed origins, methods, and headers per environment.
builder.Services.AddCorsConfiguration(builder.Environment, builder.Configuration);

// ── Rate limiting ─────────────────────────────────────────────
// Applies a sliding-window rate limiter to protect the API from abuse.
builder.Services.AddRateLimitingConfiguration(builder.Environment, builder.Configuration);

// ── HSTS ──────────────────────────────────────────────────────
// Registers HSTS options; the middleware is a no-op in Development
// so local HTTP traffic is not affected.
builder.Services.AddHstsConfiguration(builder.Environment);

// ── Application layer ─────────────────────────────────────────
// Registers MediatR handlers, FluentValidation validators, and other
// application-layer services defined in the Application project.
builder.Services.AddApplication();

// ── Infrastructure layer ──────────────────────────────────────
// Registers EF Core DbContext, repositories, identity, and other
// infrastructure-layer services defined in the Infrastructure project.
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// ── Exception handling ────────────────────────────────────────
// Registers the global exception handler and configures ProblemDetails
// so all unhandled errors return a consistent RFC 9457 response shape.
builder.Services.AddExceptionHandling();

// ── OpenAPI ───────────────────────────────────────────────────
// Generates an OpenAPI document served at /openapi/v1.json in Development.
builder.Services.AddOpenApi();

// ── OpenTelemetry tracing ─────────────────────────────────────
// Registers ASP.NET Core, HttpClient, and EF Core instrumentation and
// configures the OTLP exporter using ObservabilitySettings.
builder.Services.AddAppTracing(builder.Configuration);

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────

// Global exception handler must be the outermost middleware so it can
// catch exceptions thrown by any subsequent layer in the pipeline.
app.UseExceptionHandler();

// CorrelationIdMiddleware must run before RequestLoggingMiddleware so
// that the CorrelationId and UserId properties are already in the Serilog
// LogContext when the request log entry is written.
app.UseMiddleware<CorrelationIdMiddleware>();

// Logs method, path, status code, and elapsed time for every request.
app.UseMiddleware<RequestLoggingMiddleware>();

// ── OpenAPI (Development only) ────────────────────────────────
if (app.Environment.IsDevelopment()) app.MapOpenApi();

// ── HSTS (non-Development only) ───────────────────────────────
// Sends the Strict-Transport-Security header to instruct browsers to
// always use HTTPS; skipped in Development to allow plain HTTP.
if (!app.Environment.IsDevelopment()) app.UseHsts();

app.UseHttpsRedirection();

// ── Database migrations ───────────────────────────────────────
// Apply any pending EF Core migrations at startup so the schema is
// always up-to-date without requiring a manual migration step.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

// ── Database seeding ──────────────────────────────────────────
// Seed reference data and default records required for the application
// to operate correctly (idempotent — safe to run on every startup).
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

// ── Security & routing middleware ─────────────────────────────
app.UseCors(CorsExtension.CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
await app.RunAsync();