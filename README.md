# Zero Trust SaaS

A diploma/master thesis project demonstrating Zero Trust security principles in a multi-tenant SaaS platform built with .NET 10, Clean Architecture, DDD, React, and Material UI.

## Architecture

- **Backend**: .NET 10 Minimal API, Clean Architecture, DDD, EF Core 10 + PostgreSQL
- **Frontend**: React 19, Vite, Material UI v5, TanStack Query, Framer Motion
- **Auth**: JWT Bearer + Refresh Token rotation, BCrypt password hashing
- **Security**: MFA (TOTP), Trusted Devices, Role-Based Authorization, Audit Logs

## Running with Docker Compose

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Start everything

```bash
docker compose up --build
```

All four services start automatically. The API applies database migrations on first launch.

### URLs

| Service    | URL                              | Notes                          |
|------------|----------------------------------|--------------------------------|
| Frontend   | http://localhost:5173            | React admin portal             |
| API        | http://localhost:8080            | .NET Minimal API               |
| OpenAPI    | http://localhost:8080/openapi    | API documentation              |
| pgAdmin    | http://localhost:5050            | Database inspection UI         |
| PostgreSQL | localhost:5432                   | DB: `zero_trust_saas`          |

### pgAdmin login (local demo only)

| Field    | Value                   |
|----------|-------------------------|
| Email    | admin@zerotrust.local   |
| Password | admin                   |

To add the server in pgAdmin: Host = `postgres`, Port = `5432`, Database = `zero_trust_saas`, Username = `postgres`, Password = `postgres`.

### Stop

```bash
docker compose down
```

### Reset database (deletes all data)

```bash
docker compose down -v
```

### View logs

```bash
docker compose logs -f api
docker compose logs -f frontend
docker compose logs -f postgres
```

### Rebuild without cache

```bash
docker compose build --no-cache
docker compose up
```

## Troubleshooting

**Port already in use**
Stop any local processes using ports 5173, 8080, 5432, or 5050, then retry.

**API exits immediately**
Check logs with `docker compose logs api`. Most commonly a database connection failure — make sure the `postgres` service is healthy before `api` starts (handled automatically via `depends_on: condition: service_healthy`).

**CORS error in browser**
The `AllowedOrigins` environment variable in `docker-compose.yml` is set to `http://localhost:5173`. If you change the frontend port, update this value to match.

**JWT secret too short**
`Jwt__SecretKey` must be at least 32 characters. The default value in `docker-compose.yml` meets this requirement.

**HTTPS redirect loop**
HTTPS redirection is disabled in `Development` mode. The Docker Compose setup sets `ASPNETCORE_ENVIRONMENT=Development`, so this should not occur.

## Local Development (without Docker)

### Backend

```bash
# Create appsettings.Development.json with your local PostgreSQL credentials
# Then:
dotnet ef migrations add InitialCreate \
  --project backend/ZeroTrustSaaS.Infrastructure \
  --startup-project backend/ZeroTrustSaaS.Api

dotnet ef database update \
  --project backend/ZeroTrustSaaS.Infrastructure \
  --startup-project backend/ZeroTrustSaaS.Api

dotnet run --project backend/ZeroTrustSaaS.Api
```

### Frontend

```bash
cd frontend
cp .env.example .env.local
# Edit .env.local to set VITE_API_URL=https://localhost:7001
npm install
npm run dev
```

## Security Notes

The credentials in `docker-compose.yml` and `.env.example` are **local development/demo values only**. Do not use them in production. Replace all secrets before any public deployment.
