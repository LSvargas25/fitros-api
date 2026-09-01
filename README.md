# FitRos API

Backend for **FitRos**, a gym / fitness-coaching management platform: gym owners manage admins and coaches, coaches manage clients, workout routines and weekly training plans.

## Problem it solves

Gyms and personal trainers need a way to assign coaches to clients, build workout routines and weekly training plans, track exercises, and notify clients — with different people (owner, admin, coach) having different levels of access. FitRos models that hierarchy directly.

## Features

- **Multi-role access:** Owner (`OwnerApp`), Admin, Coach roles, each scoped to what they're allowed to manage
- **Gyms & staff:** gyms, admins, coaches
- **Clients:** client accounts and client profiles
- **Training:** exercises, workout routines, weekly training plans
- **Engagement:** notifications, a dashboard endpoint
- **Auth:** JWT authentication, email delivery (SMTP) for account-related notifications

## Architecture

Built with **Clean Architecture**, split into separate projects so business rules don't depend on infrastructure:

```
FitRos.Domain/          # entities, enums, domain rules — no external dependencies
FitRos.Application/     # use cases as commands/handlers (CQRS-style), e.g.
                          # Features/Users/Admin/CreateAdmin/{CreateAdminCommand, CreateAdminHandler}
FitRos.Infrastructure/  # EF Core, external services (email, etc.)
FitRos.API/              # ASP.NET Core Web API — controllers, JWT auth, Swagger
FitRos.Tests/            # xUnit + FluentAssertions, with test doubles
                          # (FakeCurrentUser, FakePasswordHasher, TestDbContextFactory)
```

Each use case is a command/handler pair (e.g. `CreateAdminCommand` → `CreateAdminHandler`), which keeps controllers thin and business logic independently testable — the test suite exercises handlers directly against an in-memory EF Core context and fake dependencies, not through HTTP.

## Tech stack

- ASP.NET Core Web API (C#), Clean Architecture
- Entity Framework Core, PostgreSQL
- JWT Bearer authentication, role-based access
- xUnit, FluentAssertions
- Swagger / OpenAPI

## Configuration & running locally

1. Create a PostgreSQL database and set `ConnectionStrings:DefaultConnection` via environment variables or user-secrets (do not commit real values):
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=fitros;Username=postgres;Password=your-password"
   dotnet user-secrets set "Jwt:SigningKey" "a-32-char-or-longer-secret"
   dotnet user-secrets set "Smtp:Username" "..."
   dotnet user-secrets set "Smtp:Password" "..."
   ```
2. Apply migrations and run:
   ```bash
   dotnet ef database update --project FitRos.Infrastructure --startup-project FitRos.API
   dotnet run --project FitRos.API
   ```
3. Run the test suite:
   ```bash
   dotnet test
   ```

## Status

The most architecturally complete backend in this portfolio — layered Clean Architecture with an actual automated test suite. Frontend counterpart: [fitros-web](https://github.com/LSvargas25/fitros-web).

## Future improvements

- Expand test coverage beyond the current handler-level tests (integration tests against a real/in-memory Postgres)
- API versioning

## Security note

An earlier version of this repository had a real PostgreSQL password, a real JWT signing key and a **real Gmail app password** hardcoded in `appsettings.json`. These have been replaced with placeholders that must be supplied via environment variables or user-secrets. **Because this repository was briefly public during maintenance, treat all three of those original values as compromised and rotate them** (in particular, revoke/regenerate the Gmail app password).
