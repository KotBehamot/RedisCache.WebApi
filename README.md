# RedisCache.WebApi

A .NET 8 Web API demonstrating Redis cache-aside pattern over an EF Core InMemory product catalog.

## Features
- CRUD REST endpoints for products
- Cache-aside using Redis via StackExchange.Redis (5-min TTL)
- Cache invalidation on create/update/delete
- Health checks: API and Redis
- Swagger/OpenAPI
- EF Core InMemory database with seed data
- Repository + Service layers (Clean Architecture style)
- Proper logging and graceful Redis failure handling
- Dockerfile and docker-compose (API + Redis)
- Unit tests with NUnit, NSubstitute, AutoFixture (project scaffold)

## Getting Started

Prerequisites:
- .NET 8 SDK
- Docker (optional)

Local run:
1. Update `appsettings.json` if needed. Default Redis: `localhost:6379`.
2. Run the API:
   - dotnet restore
   - dotnet run --project RedisCache.WebApi
3. Open Swagger UI at https://localhost:5001/swagger (or http://localhost:8080/swagger when using Docker).

Docker Compose:
- docker compose up --build
- API: http://localhost:8080
- Redis: localhost:6379

## API Endpoints
- GET /api/products
- GET /api/products/{id}
- POST /api/products
- PUT /api/products/{id}
- DELETE /api/products/{id}
- GET /api/cache/stats
- DELETE /api/cache/clear
- GET /health
- GET /health/redis

## Notes
- TTL is configured via `Cache:DefaultTtlSeconds` (default 300 seconds).
- Redis unavailability is handled gracefully: the API continues to function with the database.

## Tests
Test project skeleton should include NUnit, NSubstitute, and AutoFixture. Add more tests as needed.
