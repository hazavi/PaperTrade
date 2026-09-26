# PaperTrade

PaperTrade is an educational paper-trading application. Users will practice investing with virtual money while using real or delayed market prices.

The backend is organized as a modular monolith with dependencies pointing toward the Domain layer.

## Current status

Days 1 through 4 provide:

- ASP.NET Core API
- React, TypeScript, Vite, and Tailwind frontend
- Browser-to-API status check
- PostgreSQL 18 running through Docker Compose
- Entity Framework Core with Npgsql
- `User` and `Portfolio` domain entities
- One-to-one user and portfolio persistence
- Initial database migration
- Real PostgreSQL integration test
- User registration and login
- Secure password hashing
- Encrypted `HttpOnly` authentication cookies
- Protected current-user and logout endpoints
- FluentValidation request validation
- Authentication unit and integration tests
- Registration and login pages
- Protected dashboard routing
- Cookie-based frontend sessions
- Logout flow
- Client and server validation error display
- Frontend validation tests

Trading, Redis, and market data are not implemented yet.

## Technology

### Backend

- .NET 10
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- Npgsql
- FluentValidation
- ASP.NET Core cookie authentication
- xUnit

### Frontend

- React
- TypeScript
- Vite
- Tailwind CSS
- React Router
- TanStack Query
- React Hook Form
- Zod
- Vitest

### Infrastructure

- Docker
- Docker Compose

## Project structure

```text
PaperTrade/
|-- src/
|   |-- backend/
|   |   |-- PaperTrade.Api/
|   |   |-- PaperTrade.Application/
|   |   |-- PaperTrade.Domain/
|   |   `-- PaperTrade.Infrastructure/
|   `-- frontend/
|       `-- papertrade-web/
|-- tests/
|   |-- PaperTrade.UnitTests/
|   `-- PaperTrade.IntegrationTests/
|-- .env.example
|-- .gitignore
|-- docker-compose.yml
|-- PaperTrade.sln
`-- README.md
```

## Backend dependency direction

```text
Api ------------> Application ----> Domain
 |
 `--------------> Infrastructure --> Application
                                `--> Domain
```

`PaperTrade.Domain` does not depend on ASP.NET Core, Entity Framework Core, PostgreSQL, or external services.

## Prerequisites

Install:

- .NET SDK 10
- Node.js 24 or a compatible version
- npm
- Git
- Docker Desktop

## Local setup

Clone the repository and create local environment files:

```powershell
Copy-Item .env.example .env
Copy-Item .env.example src/frontend/papertrade-web/.env.development.local
```

Change `POSTGRES_PASSWORD` in `.env`.

Start PostgreSQL:

```powershell
docker compose up -d postgres
docker compose ps
```

Install and build the backend:

```powershell
dotnet restore PaperTrade.sln
dotnet build PaperTrade.sln
```

Store the database connection string with .NET User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=papertrade;Username=papertrade;Password=YOUR_LOCAL_PASSWORD" --project src/backend/PaperTrade.Api
```

Apply migrations:

```powershell
dotnet ef database update `
  --project src/backend/PaperTrade.Infrastructure `
  --startup-project src/backend/PaperTrade.Api `
  --context PaperTradeDbContext
```

Install frontend dependencies:

```powershell
Set-Location src/frontend/papertrade-web
npm install
Set-Location ../../..
```

## Run the application

Start the API:

```powershell
dotnet run --project src/backend/PaperTrade.Api --launch-profile http
```

The API runs at `http://localhost:5044`.

In another terminal, start the frontend:

```powershell
Set-Location src/frontend/papertrade-web
npm run dev
```

The frontend runs at `http://localhost:5173`.

## API endpoints

### Status

```http
GET /api/status
```

Response:

```json
{
  "status": "ok"
}
```

### Authentication

```http
POST /api/auth/register
POST /api/auth/login
POST /api/auth/logout
GET  /api/auth/me
```

Registration creates a user and a default `Paper Portfolio` with an initial and cash balance of `$100,000`.

`logout` and `me` require the encrypted `papertrade.auth` cookie. Validation failures return `400`, duplicate registration returns `409`, and invalid login returns `401`.

## Database model

A user has one portfolio. PostgreSQL enforces:

- Unique user email
- Unique portfolio `user_id`
- User and portfolio foreign-key relationship
- Cascade deletion from user to portfolio
- `numeric(18,2)` money columns
- Nonnegative cash and initial balances

## Verification

Build the backend:

```powershell
dotnet build PaperTrade.sln
```

Run the integration tests after setting `PAPERTRADE_TEST_CONNECTION_STRING`:

```powershell
dotnet test tests/PaperTrade.IntegrationTests
```

Check the frontend:

```powershell
Set-Location src/frontend/papertrade-web
npm run test
npm run lint
npm run build
```

Stop PostgreSQL without deleting its data:

```powershell
docker compose down
```

The named `postgres-data` volume preserves the database.
