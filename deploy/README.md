# Production deployment

PaperTrade can run on any container host that supports Docker Compose, or the four services can be mapped to managed container, PostgreSQL, and Redis products.

## Required external resources

- A public hostname with HTTPS
- PostgreSQL 18
- Redis 8
- A Finnhub API key
- An API container built from `src/backend/PaperTrade.Api/Dockerfile`
- A web container built with `VITE_API_BASE_URL` set to the public HTTPS API URL

The cloud load balancer or reverse proxy must terminate HTTPS and forward WebSocket connections for `/hubs/market`. PostgreSQL and Redis must remain private and must not expose public ports.

## Container-host deployment

1. Build and publish both images to a private registry. Build the web image with the final API URL:

   ```bash
   docker build -f src/backend/PaperTrade.Api/Dockerfile -t REGISTRY/papertrade-api:v1.0.0 .
   docker build -f src/frontend/papertrade-web/Dockerfile --build-arg VITE_API_BASE_URL=https://api.example.com -t REGISTRY/papertrade-web:v1.0.0 .
   docker push REGISTRY/papertrade-api:v1.0.0
   docker push REGISTRY/papertrade-web:v1.0.0
   ```

2. Copy `.env.production.example` to `.env.production` on the host and replace every example value.

3. Start the stack:

   ```bash
   docker compose --env-file .env.production -f docker-compose.production.yml up -d
   ```

4. Configure HTTPS routing to the web and API ports. Allow WebSocket upgrades for the API.

5. Verify `/health/live`, `/health/ready`, and `/metrics`. Confirm registration, a market quote, and a small buy/sell cycle before opening access.

`Database__ApplyMigrations=true` applies committed migrations when the API starts. Run one API replica during a schema upgrade, then scale out after readiness succeeds. For larger deployments, replace this with a dedicated migration job.

## Rollback

Keep the previous image tags. Restore the previous API and web tags together. Database migrations in this release are additive; do not automatically run migration `Down` methods against production data.
