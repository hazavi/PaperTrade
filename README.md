# PaperTrade

PaperTrade is an educational paper-trading application. Users will practice investing with virtual money while using real or delayed market prices.

The project is being built incrementally as a modular monolith.

## Current status

Day 1 establishes the full-stack foundation:

- ASP.NET Core API
- React and TypeScript frontend
- Tailwind CSS
- Clean backend project boundaries
- API status endpoint
- Browser-to-API connectivity
- Unit and integration test projects

Trading, authentication, persistence, and market data are not implemented yet.

## Technology

### Backend

- .NET 10
- ASP.NET Core
- xUnit

### Frontend

- React
- TypeScript
- Vite
- Tailwind CSS

PostgreSQL, Entity Framework Core, Redis, and the remaining technologies will be added during their scheduled development days.

## Project structure

```text
PaperTrade/
├── src/
│   ├── backend/
│   │   ├── PaperTrade.Api/
│   │   ├── PaperTrade.Application/
│   │   ├── PaperTrade.Domain/
│   │   └── PaperTrade.Infrastructure/
│   └── frontend/
│       └── papertrade-web/
├── tests/
│   ├── PaperTrade.UnitTests/
│   └── PaperTrade.IntegrationTests/
├── .env.example
├── .gitignore
├── PaperTrade.sln
└── README.md
```

## Backend dependency direction

```text
PaperTrade.Api
├── PaperTrade.Application
└── PaperTrade.Infrastructure
    ├── PaperTrade.Application
    └── PaperTrade.Domain

PaperTrade.Application
└── PaperTrade.Domain

PaperTrade.Domain
└── No project dependencies
```

`PaperTrade.Domain` remains independent of ASP.NET Core, databases, and external services.

## Prerequisites

Install:

- .NET SDK 10
- Node.js 24 or a compatible version
- npm
- Git

## Local setup

Clone the repository and restore the backend:

```powershell
dotnet restore PaperTrade.sln
dotnet build PaperTrade.sln
```

Install the frontend dependencies:

```powershell
Set-Location src/frontend/papertrade-web
npm install
Set-Location ../../..
```

Create the local frontend environment file:

```powershell
Copy-Item .env.example src/frontend/papertrade-web/.env.development.local
```

The example file contains backend variables that Vite ignores. Vite only exposes variables whose names begin with `VITE_`.

## Run the application

Start the backend from the repository root:

```powershell
dotnet run --project src/backend/PaperTrade.Api --launch-profile http
```

The API runs at:

```text
http://localhost:5044
```

In another terminal, start the frontend:

```powershell
Set-Location src/frontend/papertrade-web
npm run dev
```

The frontend runs at:

```text
http://localhost:5173
```

Open the frontend and confirm that its API status changes to `Connected`.

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

## Verification

Run the backend build and tests:

```powershell
dotnet build PaperTrade.sln
dotnet test PaperTrade.sln --no-build
```

Run the frontend checks:

```powershell
Set-Location src/frontend/papertrade-web
npm run lint
npm run build
```

The test projects are currently empty. Business-rule and integration tests will be added with their corresponding features.