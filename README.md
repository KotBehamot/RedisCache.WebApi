# RedisCache.WebApi

A .NET 8 Web API demonstrating Redis cache-aside pattern over an EF Core InMemory product catalog.

## Features
- CRUD REST endpoints for products
- Two-level caching:
  - L1: StrongTyped in-memory cache (StrongTypedCache)
  - L2: Redis (StackExchange.Redis) with 5-min default TTL
- Strongly-typed domain ID (`ProductId`) with routing/JSON support
- Cache invalidation on create/update/delete
- Health checks: API and Redis
- Swagger/OpenAPI
- EF Core InMemory database with seed data
- Repository + Service layers (Clean Architecture style)
- Proper logging and graceful Redis failure handling
- Dockerfile and docker-compose (API + Redis)
- Unit tests with NUnit, NSubstitute, AutoFixture

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
- GET /api/cache/l1
- GET /api/cache/l1/product/{id}
- DELETE /api/cache/l1/product/{id}
- GET /api/cache/l2/product/{id}
- DELETE /api/cache/clear
- GET /health
- GET /health/redis

## Notes
- TTL is configured via `Cache:DefaultTtlSeconds` (default 300 seconds) for L2.
- L1 TTLs configured in DI: 120s for product, 30s for list. Adjust per needs.
- Redis unavailability is handled gracefully: the API continues to function with the database.
- `ProductId` is a strongly-typed value object used end-to-end (routing, storage, caching).

## Tests
- Unit tests cover L1/L2 behavior and invalidations.
- Run: `dotnet test`.
