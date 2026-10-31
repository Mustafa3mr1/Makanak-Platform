# Makanak Real Estate Platform — Backend (.NET 10)

> ## ⚠️ NOT COMPILED, NOT TESTED
> This solution was written in a sandbox with no access to NuGet. **It has never been restored, built, migrated or run.**
> Only `Makanak.Domain` was compile-checked (on net8). Expect a few compiler errors on the first build — send them back for correction.
> No migration has been generated or applied, and no database has been created.
> No test project is included (by request).

## Solution layout
```
Makanak.sln
Makanak.Domain          entities + enums (no dependencies)
Makanak.Application     DTOs, validators, interfaces, business services (refs Domain)
Makanak.Infrastructure  EF Core, Identity, repositories, JWT, local image storage (refs Application, Domain)
Makanak.API             controllers, middleware, Swagger, composition root (refs Application, Infrastructure)
```

## Required NuGet packages (already declared in the .csproj files)
| Project | Package | Version |
|---|---|---|
| Application | FluentValidation, FluentValidation.DependencyInjectionExtensions | 11.11.0 |
| Application | Microsoft.EntityFrameworkCore (async LINQ only) | 10.0.0 |
| Application | Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions | 10.0.0 |
| Infrastructure | Microsoft.EntityFrameworkCore.SqlServer | 10.0.0 |
| Infrastructure | Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.0 |
| Infrastructure | Microsoft.IdentityModel.JsonWebTokens | 8.14.0 |
| Infrastructure | Microsoft.Extensions.Options.ConfigurationExtensions, Microsoft.Extensions.Options.DataAnnotations | 10.0.0 |
| Infrastructure | Microsoft.EntityFrameworkCore.Design (design-time only) | 10.0.0 |
| API | Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.0 |
| API | Swashbuckle.AspNetCore | 6.9.0 |
| API | Microsoft.EntityFrameworkCore.Design (design-time only) | 10.0.0 |

If a pinned version is not found, use the newest stable `10.0.x` for Microsoft packages. Swashbuckle 10.x changed to Microsoft.OpenApi 2.x (different `OpenApiSecurityScheme` API in `Program.cs`) — stay on 6.9.0 unless you want to port it.

## Prerequisites (Windows)
- .NET 10 SDK (`dotnet --version` → 10.x)
- SQL Server (LocalDB ships with Visual Studio; or SQL Server Express/Developer)
- EF tools: `dotnet tool install --global dotnet-ef`

## Setup commands (PowerShell, from the solution folder)
```powershell
dotnet restore
dotnet build

# secrets (never commit these)
cd Makanak.API
dotnet user-secrets set "Jwt:SigningKey" "<random string, 32+ chars>"
dotnet user-secrets set "Admin:Email" "admin@makanak.local"
dotnet user-secrets set "Admin:Password" "<strong password: 8+ chars, upper, lower, digit>"
dotnet user-secrets set "Admin:FullName" "Platform Administrator"
# optional: different SQL Server
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.\SQLEXPRESS;Database=MakanakDb;Trusted_Connection=True;TrustServerCertificate=True"
cd ..
```
The app refuses to start while `Jwt:SigningKey` is missing or shorter than 32 characters (by design).

## Migrations (NOT generated yet — you must create them)
```powershell
# optional: point tooling at a non-default server (default = LocalDB)
$env:MAKANAK_CONNECTION = "Server=.\SQLEXPRESS;Database=MakanakDb;Trusted_Connection=True;TrustServerCertificate=True"

dotnet ef migrations add InitialCreate --project Makanak.Infrastructure --startup-project Makanak.API --output-dir Migrations
dotnet ef database update --project Makanak.Infrastructure --startup-project Makanak.API
```
Review the generated migration: it should contain the Identity tables, `Properties`, `PropertyImages`, `Offers`, `NegotiationHistories`, `rowversion` columns on Properties and Offers, the filtered unique indexes `UX_PropertyImages_OnePrimaryPerProperty` and `UX_Offers_OneOpenOfferPerSeekerPerProperty`, and the check constraints.
Tooling uses `DesignTimeDbContextFactory`, so it does not boot the web host.

## Run
```powershell
dotnet run --project Makanak.API --launch-profile https
```
Swagger: https://localhost:7080/swagger (Development only).
In Development, `Database:InitializeOnStartup=true` applies pending migrations and seeds the roles + the initial Admin (if `Admin:*` secrets are set). Set it to `false` elsewhere and run `dotnet ef database update` as a deployment step (roles/admin seeding still requires one startup with the flag on, or the same flag in a one-off run).

## Initial Admin
Admins can never self-register. Set `Admin:Email` / `Admin:Password` (user-secrets or environment variables `Admin__Email`, `Admin__Password`) and start once with `Database:InitializeOnStartup=true`. The seeder is idempotent.

## Swagger testing guide
1. `POST /api/auth/register` as `Owner` and again (other e-mail) as `BuyerTenant`; or login as the admin.
2. `POST /api/auth/login` → copy `accessToken` → **Authorize** → paste the token only.
3. As Owner: `POST /api/properties`, then `POST /api/properties/{id}/images` (JPEG/PNG/WebP ≤ 5 MB).
4. Anonymous: `GET /api/properties?city=Cairo&sortBy=Price&sortDirection=Asc&pageNumber=1&pageSize=10`.
5. As Buyer: `POST /api/properties/{id}/offers` `{ "price": 900000 }`.
6. As Owner: `GET /api/owner/offers`, then `POST /api/offers/{id}/counter` / `accept` / `reject`.
7. `GET /api/offers/{id}/history` for the audit trail.

## Endpoints
| Area | Endpoint | Access |
|---|---|---|
| Auth | `POST /api/auth/register`, `POST /api/auth/login` | public |
| Properties | `GET /api/properties`, `GET /api/properties/{id}` | public |
| | `POST /api/properties`, `PUT /api/properties/{id}`, `PUT /api/properties/{id}/status` | Owner (own) |
| | `DELETE /api/properties/{id}` (soft) | Owner (own) / Admin |
| Images | `POST /api/properties/{id}/images`, `PUT …/images/{imageId}/primary`, `DELETE …/images/{imageId}` | Owner (own) |
| Offers | `POST /api/properties/{propertyId}/offers` | BuyerTenant |
| | `GET /api/offers/{id}`, `GET /api/offers/{id}/history` | participants / Admin |
| | `GET /api/offers/my-offers` | BuyerTenant |
| | `GET /api/owner/offers` | Owner |
| | `POST /api/offers/{id}/accept`, `/reject`, `/counter` | the party whose turn it is |
| User | `GET/PUT /api/user/me` | authenticated |
| | `GET /api/user/owner/dashboard`, `GET /api/user/buyer/dashboard` | Owner / BuyerTenant |
| Admin | `GET /api/admin/users`, `GET …/users/{id}`, `POST …/users/{id}/deactivate|reactivate` | Admin |
| | `GET /api/admin/properties`, `POST …/properties/{id}/deactivate|reactivate`, `GET /api/admin/stats` | Admin |

## Design documentation
See [docs/README.md](docs/README.md): ERD differences, enums, state machine, concurrency strategy, deletion policy, security notes.
