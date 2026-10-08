# FitRos API

Backend for **FitRos**, a gym / fitness-coaching management platform: gym owners manage admins and coaches, coaches manage clients, workout routines and weekly training plans.

## Live demo

| | |
|---|---|
| **Web app** | https://fitros-web.onrender.com — use the **"Probar demo"** buttons on the login page, no sign-up needed |
| **API** | https://fitros-api.onrender.com |
| **Health** | [`/health`](https://fitros-api.onrender.com/health) (process up) · [`/health/ready`](https://fitros-api.onrender.com/health/ready) (database reachable) |

> **Heads-up:** both services run on Render's free tier and sleep when idle. The first request can take **up to ~1 minute** while the server wakes up; after that it's fast.

### Demo accounts

All demo accounts share the password **`FitRos#2026`**. They belong to a fictional gym, *Iron Valley Fitness*, pre-loaded with coaches, clients, exercises, routines, weekly plans, meal plans, progress measurements, workout history and notifications.

| Role | Email | What to try |
|------|-------|-------------|
| Owner (platform) | `owner@fitros.demo` | **Dashboard** with platform-wide totals; **Admin / Coach / Client Management**: gyms, assigning admins and coaches |
| Admin (gym) | `admin@fitros.demo` | **Dashboard** for the gym; **Clients** (view, add a client); **Exercises** and **Foods** catalogs |
| Coach | `coach@fitros.demo` | **Clients** roster; **Routines** (open the draft, edit it, publish it); **Weekly Plans** and **Meal Plans**; **Reports** and **Client Measures** for a client's progress |
| Client | `client@fitros.demo` | **My Training** (start today's workout, log sets, finish it); **My Weekly Plan**; **My Nutrition**; **My Progress** and **My Measures** (add a measurement, watch the trend) |

The demo data is created by `DemoDataSeeder` (`FitRos.Infrastructure/Persistence/Seeding`) on boot when `Seed__Demo=true`. It's idempotent: the gym and its content are created once, and on every boot the demo logins are re-activated and their password reset, so one visitor can't lock out the next. Visitors share the same data, so changes they make stay until someone cleans them up.

### Resumen en español

Demo en vivo: **https://fitros-web.onrender.com** → en el login, botones **"Probar demo"** (un clic por rol, sin registrarse). Todas las cuentas usan la contraseña **`FitRos#2026`**: `owner@fitros.demo` (dueño de la plataforma), `admin@fitros.demo` (administrador del gimnasio), `coach@fitros.demo` (coach) y `client@fitros.demo` (cliente). El servidor es gratuito y se duerme: **la primera carga puede tardar hasta 1 minuto**. Los datos demo los crea `DemoDataSeeder` al arrancar si `Seed__Demo=true`, sin duplicarse en cada reinicio.


## Problem it solves

Gyms and personal trainers need a way to assign coaches to clients, build workout routines and weekly training plans, track exercises, and notify clients — with different people (owner, admin, coach) having different levels of access. FitRos models that hierarchy directly.

## Features

- **Multi-role access:** Owner (`OwnerApp`), Admin, Coach and Client roles, each scoped to what they're allowed to manage
- **Gyms & staff:** gyms, admins, coaches
- **Clients:** client accounts and client profiles
- **Training:** exercises, workout routines, weekly training plans, workout sessions
- **Nutrition:** food catalog and per-client meal plans
- **Engagement:** notifications, a dashboard endpoint
- **Ops:** `/health` and `/health/ready` probes, opt-in Swagger in production (`Swagger__Enabled=true`), opt-in demo data (`Seed__Demo=true`)
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
   To get the demo gym and accounts locally, run with `Seed__Demo=true` (env var) or `dotnet user-secrets set "Seed:Demo" "true"`.
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
