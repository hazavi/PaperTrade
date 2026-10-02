# PaperTrade

PaperTrade is an educational paper-trading application. Users will practice investing with virtual money while using real or delayed market prices.

The backend is organized as a modular monolith with dependencies pointing toward the Domain layer.

## Current status

Week 1 provides:

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
- Multi-stage API and frontend Docker images
- Nginx static frontend hosting with SPA route fallback
- Redis development service
- Four-service Docker Compose environment
- Container health checks and dependency ordering
- Persistent PostgreSQL data and authentication keys
- Consistent ProblemDetails error responses
- Safe global exception handling
- Request and response logging without body or cookie logging
- Automated error-contract integration tests
- Playwright authentication end-to-end test

Trading, Redis-backed application caching, and market data are not implemented yet.

## Technology

### Backend

- .NET 10
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- Npgsql
- FluentValidation
- ASP.NET Core cookie authentication
- ASP.NET Core ProblemDetails
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
- Playwright

### Infrastructure

- Docker
- Docker Compose
- PostgreSQL
- Redis
- Nginx

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
|-- .dockerignore
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

## Run with Docker

Create the local environment file:

```powershell
Copy-Item .env.example .env
```

Change `POSTGRES_PASSWORD` in `.env`, then build and start the complete application:

```powershell
docker compose up --detach --build
docker compose ps
```

The frontend runs at `http://localhost:5173`, and the API runs at `http://localhost:5044`.

View container logs:

```powershell
docker compose logs --follow
```

Stop the application without deleting persistent data:

```powershell
docker compose down
```

The `postgres-data` volume preserves database records. The `api-data-protection` volume preserves the keys used to encrypt authentication cookies.

## Run locally for development

Clone the repository and create local environment files:

```powershell
Copy-Item .env.example .env
Copy-Item .env.example src/frontend/papertrade-web/.env.development.local
```

Change `POSTGRES_PASSWORD` in `.env`.

Start PostgreSQL and Redis:

```powershell
docker compose up --detach postgres redis
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

## API error contract

API errors use `application/problem+json`. Each response includes a stable error type, HTTP status, request path, and trace identifier:

```json
{
  "type": "urn:papertrade:error:validation",
  "title": "Validation failed.",
  "status": 400,
  "instance": "/api/auth/register",
  "traceId": "0HN...",
  "errors": {
    "email": [
      "Enter a valid email address."
    ]
  }
}
```

Unexpected exceptions are logged by the API, while clients receive a generic `500` response without stack traces or internal exception details.

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

Run backend unit tests:

```powershell
dotnet test tests/PaperTrade.UnitTests
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

Run the browser test against the Docker stack:

```powershell
docker compose up --detach --build

Set-Location src/frontend/papertrade-web
npx playwright install chromium
npm run test:e2e
Set-Location ../../..
```

The Playwright test registers a unique user, verifies the `$100,000` dashboard balance, logs out, and logs back in.

Stop the containers without deleting persistent data:

```powershell
docker compose down
```

The named `postgres-data` and `api-data-protection` volumes remain available after the containers stop.
