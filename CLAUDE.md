# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

CS Sistemas is a SaaS platform for professional appointment scheduling. Professionals manage their business, services, and working hours; customers book appointments publicly and pay via PIX, credit card, or Mercado Pago.

## Development Commands

### Backend (.NET 8 API)

```bash
cd CSSistemas.API
dotnet restore
dotnet build
dotnet run          # API at http://localhost:5264, Swagger at /swagger
```

**First-time setup:** Copy `appsettings.Development.example.json` → `appsettings.Development.json` and fill in your PostgreSQL connection string. The API auto-runs SQL migration scripts from `CSSistemas.API/Scripts/` (001–019) on startup via `DatabaseScriptRunner`.

### Frontend (React + Vite)

```bash
cd cssistemas-web
npm install
npm run dev         # http://localhost:5173, proxies /api → localhost:5264
npm run build       # output to cssistemas-web/dist/
```

**Production build note:** The `.csproj` has a pre-build target that runs `npm run build` and copies `dist/` to `CSSistemas.API/wwwroot` via robocopy. The API then serves the SPA as static files.

### Testing Endpoints

Use Swagger UI (`http://localhost:5264/swagger`) or the `CSSistemas.API.http` file (VS Code REST Client format).

There are no automated test projects in this repository.

## Architecture

Clean Architecture with 4 layers:

```
CSSistemas.Domain       → Entities, enums (no dependencies)
CSSistemas.Application  → DTOs, validators (FluentValidation), interfaces, configuration classes
CSSistemas.Infrastructure → EF Core repositories, email/payment/auth services
CSSistemas.API          → ASP.NET Core controllers, middleware, mappers, startup
cssistemas-web/         → React 18 + TypeScript + Tailwind CSS (Vite)
```

**Dependency direction:** API → Infrastructure → Application → Domain

## Key Patterns

**Database:** PostgreSQL via EF Core 8 (Npgsql). No EF migrations — raw SQL scripts in `CSSistemas.API/Scripts/` named `001_...sql` through `019_...sql`, executed in order on startup. When adding schema changes, create the next numbered script and register it if needed in `DatabaseScriptRunner.cs`.

**DTOs:** Defined in `CSSistemas.Application/DTOs/`, organized by domain (Auth, Business, Service, Appointment, etc.) with separate `*Request` and `*Response` classes. Validators live alongside them.

**Repository pattern:** Interfaces in `CSSistemas.Application/Interfaces/`, implementations in `CSSistemas.Infrastructure/Repositories/`. Infrastructure DI is registered in `CSSistemas.Infrastructure/DependencyInjection.cs`.

**Email:** Async via `Channel<T>` queue. `EmailQueueHostedService` runs as a background worker. Supports SMTP or Resend provider (configured via `Email:Provider`).

**Authentication:** JWT bearer tokens. `Admin` role is checked via `[Authorize(Roles = "Admin")]`. Public booking endpoints are whitelisted in the auth middleware.

**Frontend API calls:** All HTTP requests go through `cssistemas-web/src/api/client.ts`. No separate API modules per feature — one central client.

**Frontend state:** `AuthContext` (user session/token) and `ValuesVisibilityContext` (toggle monetary value display) via React Context API.

## Multi-tenancy Model

- User → owns multiple Businesses
- Business → has Services, Appointments, Clients, BusinessHours, Notifications, SystemMessages
- Each Business has a unique `slug` used for the public booking URL
- Appointment availability is calculated real-time based on BusinessHours + existing Appointments + Service duration

## Configuration

All secrets go in `appsettings.Development.json` (gitignored). Key sections:
- `ConnectionStrings:DefaultConnection` — PostgreSQL
- `Jwt:Secret` — min 32 chars signing key
- `Email` — Provider ("Smtp" or "Resend"), credentials
- `Payment` — PIX key, MercadoPago access token
- `Admin` — seeded admin credentials
- `BaseBookingUrl` — frontend URL for public booking links
