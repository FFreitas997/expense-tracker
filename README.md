# 💸 Expense Tracker API

> **A production-grade, multi-user REST API** built with **.NET 10** for tracking personal expenses, managing budgets, scheduling recurring charges, generating monthly summaries, and exporting data to CSV.

[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13-blueviolet.svg)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![EF Core](https://img.shields.io/badge/EF%20Core-10.0-green.svg)](https://learn.microsoft.com/en-us/ef/core/)

---

## 📑 Table of Contents

1. [Project Overview](#-project-overview)
2. [Architecture](#-architecture)
3. [Project Structure](#-project-structure)
4. [Domain Model](#-domain-model)
5. [Technologies & Libraries](#-technologies--libraries)
6. [Design Patterns](#-design-patterns)
7. [SOLID Principles](#-solid-principles)
8. [ACID Guarantees](#-acid-guarantees)
9. [Security](#-security)
10. [Observability](#-observability)
11. [Background Jobs](#-background-jobs)
12. [Testing](#-testing)
13. [Getting Started](#-getting-started)
14. [Configuration Reference](#-configuration-reference)
15. [API Overview](#-api-overview)

---

## 📌 Project Overview

Expense Tracker is a **RESTful API** designed for managing personal finances across multiple users. It supports:

| Feature | Description |
|---|---|
| 👤 **Multi-user** | Individual accounts with role-based access (Admin / User) |
| 📂 **Categories** | Custom and default spending categories per user |
| 💰 **Expenses** | Full CRUD with currency, payment method, and date tracking |
| 🔁 **Recurring Expenses** | Automated scheduling via Quartz.NET jobs |
| 🎯 **Budgets** | Period-based budget limits with configurable frequency |
| 📊 **Reports** | Monthly summaries and CSV export |
| 🔐 **Security** | JWT authentication, rate limiting, CORS, HSTS, TLS 1.2+ |
| 📡 **Observability** | Structured logging (Serilog) + distributed tracing (OpenTelemetry) |
| 📧 **Email** | Transactional emails via MailKit |

---

## 🏛 Architecture

The solution follows **Clean Architecture** (also known as Onion Architecture), ensuring a strict **dependency rule**: inner layers never depend on outer ones. The API is organised into four projects that map directly to the architectural rings:

```
┌─────────────────────────────────────────────┐
│                    API                       │  ← Presentation layer
│  Controllers · Middlewares · Filters ·       │
│  Security · Observability · Extensions       │
├─────────────────────────────────────────────┤
│                Application                   │  ← Use-case / business logic layer
│  Services · DTOs · Validators · Mappings ·  │
│  Common (Result, Errors)                     │
├─────────────────────────────────────────────┤
│               Infrastructure                 │  ← Infrastructure / persistence layer
│  EF Core · Repositories · Identity ·        │
│  Cache · Email · Jobs · UnitOfWork · Seeds  │
├─────────────��───────────────────────────────┤
│                  Domain                      │  ← Core domain layer (no dependencies)
│  Entities · Enums                            │
└─────────────────────────────────────────────┘
```

### Dependency Flow

```
API  ──depends on──►  Application  ──depends on──►  Infrastructure  ──depends on──►  Domain
```

The **Domain** project has **zero external dependencies** — it is a pure C# class library containing only entities and enums.

---

## 📁 Project Structure

```
ExpenseTracker/
├── API/                          # ASP.NET Core Web API (entry point)
│   ├── Controllers/
│   │   ├── BackOffice/V1/        # Admin-only endpoints (e.g. CategoryController)
│   │   └── FrontOffice/V1/       # User-facing endpoints (Auth, Category, ...)
│   ├── Exceptions/               # Global exception handler (RFC 9457 ProblemDetails)
│   ├── Extensions/               # Service registration helpers
│   ├── Filters/                  # ValidationFilter (FluentValidation hook)
│   ├── Middlewares/              # CorrelationIdMiddleware, RequestLoggingMiddleware
│   ├── Observability/            # Serilog + OpenTelemetry configuration
│   ├── Security/                 # JWT auth, authorization policies, CORS, rate limiting
│   └── Program.cs                # Application bootstrap
│
├── Application/                  # Use-case layer
│   ├── Common/                   # Result<T> monad, Error types
│   ├── DTOs/                     # Request / response data transfer objects
│   ├── Exceptions/               # Application-level exceptions
│   ├── Mappings/                 # AutoMapper profiles
│   ├── Services/
│   │   ├── Interfaces/           # IAuthService, IExpenseService, IReportService, ...
│   │   └── Implementations/      # Concrete service implementations
│   ├── Validators/               # FluentValidation validators
│   └── Registration.cs           # DI registration for the Application layer
│
├── Infrastructure/               # Infrastructure / persistence layer
│   ├── Cache/                    # IMemoryCache abstraction + implementation
│   ├── Data/                     # AppDbContext (EF Core), entity configurations
│   ├── Email/                    # MailKit email service
│   ├── Identity/                 # ASP.NET Core Identity configuration
│   ├── Jobs/                     # Quartz.NET background jobs (RecurringExpenseJob)
│   ├── Migrations/               # EF Core database migrations
│   ├── Repositories/
│   │   ├── Interfaces/           # IExpenseRepository, IBudgetRepository, ...
│   │   └── Implementations/      # EF Core + Dapper concrete repositories
│   ├── Seeds/                    # RoleSeeder, DefaultCategorySeeder, DatabaseSeeder
│   ├── UnitOfWork/               # IUnitOfWork pattern implementation
│   └── Registration.cs           # DI registration for the Infrastructure layer
│
├── Domain/                       # Core domain (no external dependencies)
│   ├── Entities/                 # BaseEntity, User, Expense, Category, Budget, RecurringExpense
│   └── Enums/                    # BudgetPeriod, Currency, PaymentMethod, RecurringFrequency, UserRole, UserState
│
├── Unit/                         # Unit test project
└── Integration/                  # Integration test project
```

---

## 🗂 Domain Model

### Entities

| Entity | Description |
|---|---|
| `User` | Extends ASP.NET Identity; has a `UserState` and `UserRole` |
| `Expense` | Tracks amount, date, description, `PaymentMethod`, `Currency`, linked to `Category` |
| `Category` | Named grouping for expenses; can be default (system) or user-defined |
| `Budget` | Spending limit for a user over a `BudgetPeriod` |
| `RecurringExpense` | Template for automatic expense creation at a `RecurringFrequency` |
| `BaseEntity` | Abstract base with `Id`, `CreatedAt`, and `UpdatedAt` audit fields |

### Enums

| Enum | Values |
|---|---|
| `UserRole` | `Admin`, `User` |
| `UserState` | `Active`, `Inactive` |
| `Currency` | `USD`, `EUR`, `GBP`, ... |
| `PaymentMethod` | `Cash`, `CreditCard`, `DebitCard`, ... |
| `BudgetPeriod` | `Weekly`, `Monthly`, `Yearly` |
| `RecurringFrequency` | `Daily`, `Weekly`, `Monthly`, `Yearly` |

---

## 🛠 Technologies & Libraries

### Core Framework
| Technology | Version | Purpose |
|---|---|---|
| **.NET** | 10.0 | Target framework |
| **ASP.NET Core** | 10.0 | Web API host |
| **C#** | 13 | Language |

### Data Access
| Library | Version | Purpose |
|---|---|---|
| **Entity Framework Core** | 10.0.8 | ORM, migrations, DbContext |
| **EF Core SQL Server** | 10.0.8 | SQL Server provider |
| **Dapper** | 2.1.79 | Lightweight micro-ORM for raw/complex queries |

### Identity & Security
| Library | Version | Purpose |
|---|---|---|
| **ASP.NET Core Identity** | 10.0.8 | User management, password hashing, lockout |
| **JwtBearer** | 10.0.8 | JWT token validation middleware |

### API
| Library | Version | Purpose |
|---|---|---|
| **Asp.Versioning.Mvc** | 10.0.0 | URL-segment API versioning (`/api/v1/...`) |
| **Microsoft.AspNetCore.OpenApi** | 10.0.8 | OpenAPI document generation |

### Application
| Library | Version | Purpose |
|---|---|---|
| **AutoMapper** | 16.1.1 | Object-to-object mapping (entity ↔ DTO) |
| **FluentValidation** | 12.1.1 | Declarative input validation rules |

### Background Jobs
| Library | Version | Purpose |
|---|---|---|
| **Quartz.NET** | 3.18.1 | Job scheduling for recurring expense automation |

### Email
| Library | Version | Purpose |
|---|---|---|
| **MailKit** | 4.17.0 | SMTP email delivery |

### Observability
| Library | Version | Purpose |
|---|---|---|
| **Serilog.AspNetCore** | 10.0.0 | Structured logging |
| **Serilog.Sinks.Console/File** | latest | Log output targets |
| **Serilog.Enrichers.*** | latest | CorrelationId, Thread, Environment enrichers |
| **OpenTelemetry** | 1.15.x | Distributed tracing |
| **OpenTelemetry.Instrumentation.AspNetCore** | 1.15.x | HTTP request traces |
| **OpenTelemetry.Instrumentation.EntityFrameworkCore** | 1.15.x | EF Core query traces |
| **OpenTelemetry.Exporter.OpenTelemetryProtocol** | 1.15.x | OTLP trace export |

---

## 🎨 Design Patterns

### 1. Repository Pattern
Each domain aggregate has its own repository interface (`IExpenseRepository`, `IBudgetRepository`, etc.) with a concrete EF Core implementation. This decouples business logic from data access and makes substitution (e.g. in-memory for tests) trivial.

### 2. Unit of Work Pattern
`IUnitOfWork` wraps multiple repository operations in a single transaction, ensuring atomicity. Services call `SaveChangesAsync()` only through the Unit of Work, never directly via the DbContext.

### 3. Result Pattern (Railway-oriented programming)
`Result<T>` and `Result` are sealed monad types used throughout the Application layer instead of throwing exceptions for expected failures. They expose `.Map()`, `.Bind()`, `.MapAsync()`, `.BindAsync()`, and `.Match()` helpers enabling clean, chainable error propagation without `try/catch` noise.

```csharp
// Example: chaining operations without exceptions
var result = await GetUserAsync(id)
    .BindAsync(user => CreateExpenseAsync(user, request))
    .MapAsync(expense => _mapper.Map<ExpenseDto>(expense));
```

### 4. Service Layer Pattern
Application logic is encapsulated in dedicated services (`IAuthService`, `IExpenseService`, `IReportService`, etc.) that are injected into controllers. Controllers are kept thin — they translate HTTP requests into service calls and HTTP responses.

### 5. Options Pattern
All configuration sections (`Jwt`, `Database`, `RateLimiter`, `Identity`, `InMemoryCache`, `ObservabilitySettings`, `Quartz`) are bound to strongly-typed POCO classes via `IOptions<T>`, eliminating magic strings throughout the codebase.

### 6. Decorator / Middleware Pattern
Cross-cutting concerns are implemented as ASP.NET Core middleware:
- `CorrelationIdMiddleware` — injects a unique `X-Correlation-Id` into every request and the Serilog `LogContext`.
- `RequestLoggingMiddleware` — logs method, path, status code, and elapsed time for every HTTP request.

### 7. Strategy / Extension Method Pattern
Security, CORS, rate limiting, HSTS, Kestrel TLS, and tracing are each encapsulated in dedicated extension classes (`AddJwtAuthentication`, `AddCorsConfiguration`, `AddRateLimitingConfiguration`, `AddAppTracing`, etc.), keeping `Program.cs` declarative and readable.

### 8. Seeder / Data Initialiser Pattern
`DatabaseSeeder` orchestrates `RoleSeeder` and `DefaultCategorySeeder`, each idempotent — safe to run on every application startup. This ensures the database is always bootstrapped with required reference data.

### 9. Factory Method (Result)
`Result<T>.Success(value)` and `Result<T>.Failure(error)` are static factory methods that control the construction of result objects and enforce invariants (a failed result cannot expose a value).

### 10. Global Exception Handler (RFC 9457)
A centralised exception handler maps all unhandled exceptions to consistent `ProblemDetails` responses, ensuring no raw stack traces leak to clients and every error follows the same contract.

---

## 🧱 SOLID Principles

### S — Single Responsibility Principle
Every class has a single reason to change:
- `ExpenseService` handles only expense use-cases.
- `RecurringExpenseJob` is exclusively responsible for materialising recurring expenses.
- `CorrelationIdMiddleware` handles only correlation ID injection.
- Validators (`FluentValidation`) live in their own dedicated classes, separate from services.

### O — Open/Closed Principle
- New features are added by **extending** (new service, new validator, new repository) rather than modifying existing ones.
- The `Result<T>` monad can be extended with new monadic helpers without changing existing ones.
- API versioning (`/api/v1/`, `/api/v2/`) allows new controller versions without breaking existing clients.

### L ��� Liskov Substitution Principle
- All repository implementations honour the contracts defined by their interfaces (`IExpenseRepository`, `IBudgetRepository`, etc.).
- Any implementation can be substituted (e.g. an in-memory fake for tests) without callers needing to change.

### I — Interface Segregation Principle
- Repository interfaces are split by aggregate root (`IUserRepository`, `IExpenseRepository`, `ICategoryRepository`, `IBudgetRepository`, `IRecurringExpenseRepository`) rather than one large monolithic repository.
- `ICacheRepository` is a focused interface for cache operations only.
- `IAppDbContext` exposes only what the application needs from the DbContext.

### D — Dependency Inversion Principle
- All services depend on **abstractions** (interfaces), never on concrete classes.
- DI containers (`Registration.cs` in each layer) wire up the concrete implementations.
- The Application layer has no direct reference to EF Core, SQL Server, or MailKit — it only knows about its own interfaces.

---

## 🔒 ACID Guarantees

ACID properties are maintained through the following mechanisms:

| Property | How it is enforced |
|---|---|
| **Atomicity** | `IUnitOfWork` wraps all repository operations for a given request in a single `SaveChangesAsync()` call. Either all changes persist or none do. |
| **Consistency** | FluentValidation ensures only valid data ever reaches the domain. EF Core entity configurations enforce constraints (non-null, max-length, FK relationships) at the database level. ASP.NET Core Identity enforces password and account rules. |
| **Isolation** | SQL Server's default transaction isolation level (Read Committed) is used. EF Core tracks changes per DbContext scope (scoped lifetime), preventing cross-request contamination. |
| **Durability** | EF Core persists all committed transactions to SQL Server. EF Core Migrations manage schema evolution without data loss. Quartz.NET persistent store ensures scheduled jobs survive process restarts. |

---

## 🔐 Security

| Mechanism | Implementation |
|---|---|
| **Authentication** | JWT Bearer tokens with configurable expiry, issuer, and audience |
| **Authorization** | Role-based policies (`Admin`, `User`) via `AddAppAuthorization()` |
| **Password policy** | Minimum 12 characters, requires digit, upper/lowercase, non-alphanumeric, 6 unique characters |
| **Account lockout** | 5 failed attempts → 30-minute lockout |
| **TLS** | TLS 1.2+ enforced on Kestrel; HTTPS redirection enabled |
| **HSTS** | Strict-Transport-Security header sent in non-Development environments |
| **CORS** | Per-environment allowed origins, methods, and headers |
| **Rate limiting** | Sliding-window limiter: 60 requests / minute per client |
| **Sensitive data** | `Server` response header suppressed; sensitive data logging disabled in production |

---

## 📡 Observability

### Structured Logging — Serilog
- Serilog is bootstrapped **before any other service** to capture all startup activity.
- Every log entry is enriched with `CorrelationId`, `ThreadId`, and `EnvironmentName`.
- Sinks: console (development) and rolling file (`logs/expense-tracker-.log`).

### Distributed Tracing — OpenTelemetry
- ASP.NET Core HTTP requests, outbound `HttpClient` calls, and EF Core SQL queries are all instrumented.
- Traces are exported via **OTLP** to any compatible backend (Jaeger, Tempo, etc.) at the configured endpoint (`http://localhost:4317` by default).
- A console exporter is available for development.

### Request Logging
`RequestLoggingMiddleware` logs the HTTP method, path, status code, and elapsed time for every request, giving a quick audit trail without needing a full APM setup.

---

## ⏰ Background Jobs

Quartz.NET is used for server-side job scheduling:

| Job | Schedule | Description |
|---|---|---|
| `RecurringExpenseJob` | Configurable (cron / simple trigger) | Iterates all active `RecurringExpense` records and materialises the next due `Expense` entries, sending email notifications as appropriate |

Quartz is configured with:
- **Persistent store** — jobs survive application restarts.
- **Controlled concurrency** — max 5 concurrent jobs.
- **Graceful shutdown** — waits for running jobs to complete before stopping.

---

## 🧪 Testing

The solution includes two test projects:

### Unit Tests (`Unit/`)
- Tests individual classes in isolation.
- Domain entities, `Result<T>` monad, validators, and service logic can be tested without any infrastructure dependencies.
- Recommended libraries to add: `xUnit`, `Moq` / `NSubstitute`, `FluentAssertions`.

### Integration Tests (`Integration/`)
- Tests the full request pipeline including controllers, services, repositories, and the database.
- Recommended approach: use `WebApplicationFactory<Program>` with a real or in-memory SQL Server / SQLite database spun up via Testcontainers.
- Validates HTTP responses, database side-effects, and cross-layer correctness.

### Testing Strategy
```
┌──────────────────────────────────────────────┐
│             Integration Tests                 │  Full stack: HTTP → DB
│  WebApplicationFactory + Testcontainers       │
├──────────────────────────────────────────────┤
│               Unit Tests                      │  Isolated classes
│  Domain, Services, Validators (mocked deps)   │
└──────────────────────────────────────────────┘
```

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server) (or SQL Server Express / LocalDB for local dev)
- *(Optional)* An OTLP-compatible tracing backend (Jaeger, Grafana Tempo, etc.)
- *(Optional)* SMTP server for email features

### 1. Clone the repository

```bash
git clone https://github.com/FFreitas997/expense-tracker.git
cd expense-tracker
```

### 2. Configure the application

Copy `appsettings.Development.json` and update the required values:

```bash
cd ExpenseTracker/API
```

Edit `appsettings.Development.json`:

```json
"Database": {
  "ConnectionString": "Server=localhost;Database=expense_tracker_db;Trusted_Connection=True;TrustServerCertificate=True;"
},
"Jwt": {
  "SecretKey": "<your-strong-secret-key-min-32-chars>",
  "Issuer": "expense-tracker-api",
  "Audience": "expense-tracker-api"
}
```

### 3. Run the application

```bash
cd ExpenseTracker/API
dotnet run
```

On startup the application will:
1. Apply any pending EF Core migrations automatically.
2. Seed default roles and categories.
3. Start Quartz.NET background scheduler.

### 4. Explore the API

OpenAPI documentation is available in Development mode at:

```
https://localhost:<port>/openapi/v1.json
```

### 5. Run the tests

```bash
# Unit tests
dotnet test ExpenseTracker/Unit

# Integration tests
dotnet test ExpenseTracker/Integration
```

---

## ⚙️ Configuration Reference

| Section | Key | Default | Description |
|---|---|---|---|
| `Database` | `ConnectionString` | — | SQL Server connection string |
| `Database` | `MaxRetryCount` | `3` | EF Core resilience retry count |
| `Database` | `CommandTimeout` | `30` | SQL command timeout (seconds) |
| `Jwt` | `SecretKey` | — | HMAC signing key (keep secret!) |
| `Jwt` | `ExpirationMinutes` | `60` | Access token lifetime |
| `Jwt` | `RefreshTokenDays` | `7` | Refresh token lifetime |
| `Identity` | `MaxFailedAccessAttempts` | `5` | Lockout threshold |
| `Identity` | `DefaultLockoutTimeSpan` | `00:30:00` | Lockout duration |
| `RateLimiter` | `PermitLimit` | `60` | Requests per window |
| `RateLimiter` | `WindowMinutes` | `1` | Sliding window size |
| `InMemoryCache` | `CacheSizeLimit` | `1000` | Max cached entries |
| `Quartz` | `UsePersistentStore` | `true` | Persist job state across restarts |
| `ObservabilitySettings` | `Otlp.Endpoint` | `http://localhost:4317` | OTLP exporter target |

---

## 📋 API Overview

All endpoints are versioned under `/api/v1/`.

### FrontOffice (User-facing)

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/v1/auth/register` | Register a new user |
| `POST` | `/api/v1/auth/login` | Authenticate and receive JWT |
| `POST` | `/api/v1/auth/refresh` | Refresh an access token |
| `GET` | `/api/v1/categories` | List user categories |
| `POST` | `/api/v1/categories` | Create a category |
| `PUT` | `/api/v1/categories/{id}` | Update a category |
| `DELETE` | `/api/v1/categories/{id}` | Delete a category |
| `GET` | `/api/v1/expenses` | List expenses (paginated) |
| `POST` | `/api/v1/expenses` | Create an expense |
| `PUT` | `/api/v1/expenses/{id}` | Update an expense |
| `DELETE` | `/api/v1/expenses/{id}` | Delete an expense |
| `GET` | `/api/v1/budgets` | List budgets |
| `POST` | `/api/v1/budgets` | Create a budget |
| `GET` | `/api/v1/recurring` | List recurring expenses |
| `POST` | `/api/v1/recurring` | Create a recurring expense |
| `GET` | `/api/v1/reports/monthly` | Monthly summary report |
| `GET` | `/api/v1/reports/export` | Export expenses to CSV |

### BackOffice (Admin-only)

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/admin/categories` | List all system categories |
| `POST` | `/api/v1/admin/categories` | Create a system/default category |
| `PUT` | `/api/v1/admin/categories/{id}` | Update a system category |
| `DELETE` | `/api/v1/admin/categories/{id}` | Delete a system category |

---

## 📄 License

This project is licensed under the **Apache License 2.0** — see the [LICENSE](LICENSE) file for details.

---

<div align="center">
  Built with ❤️ using .NET 10 · Clean Architecture · SOLID principles
</div>
