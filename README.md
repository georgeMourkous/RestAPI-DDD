# RestApiDdd

.NET 10 REST API solution structured around Domain Driven Design.

## Projects

- `RestApiDdd.Api`: ASP.NET Core REST API, JWT authentication, role authorization filter, exception middleware.
- `RestApiDdd.Service`: DTOs, mapping, repository abstractions, unit of work contract, CQRS commands and queries.
- `RestApiDdd.Domain`: package aggregate, entities, business validation, domain events.
- `RestApiDdd.Infrastructure`: EF Core 10 SQL Server DbContext, mappings, repositories, cached lookups, migrations.

## Package Resource

The API exposes a single package controller at `api/packages`:

- `GET /api/packages`
- `GET /api/packages/{id}`
- `POST /api/packages`
- `PUT /api/packages/{id}`
- `PATCH /api/packages/{id}`
- `DELETE /api/packages/{id}`

All endpoints require a valid JWT. Read operations allow `Admin`, `PackageManager`, or `PackageReader`; write operations allow `Admin` or `PackageManager`; delete requires `Admin`.

## Database

The default connection string points at LocalDB:

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=RestApiDdd;Trusted_Connection=True;TrustServerCertificate=True;"
```

Apply the initial code-first migration:

```powershell
dotnet dotnet-ef database update --project RestApiDdd.Infrastructure\RestApiDdd.Infrastructure.csproj --startup-project RestApiDdd.Infrastructure\RestApiDdd.Infrastructure.csproj --context ApplicationDbContext
```

The migration seeds one `PackageCategory` and one `Service` lookup record so package creation can work immediately.

## Build

```powershell
dotnet restore RestApiDdd.slnx
dotnet build RestApiDdd.slnx
```
