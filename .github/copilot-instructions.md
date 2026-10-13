# Copilot instructions for koochino

## Build, test, and validation

This repo is a .NET 8 solution (`MyShopSolution.sln`) with an xUnit test project (`MyShop.Tests`). The supported repo-level validation commands are:

- Build the full solution:
  `dotnet build MyShopSolution.sln --nologo`
- Run the full test suite:
  `dotnet test MyShop.Tests/MyShop.Tests.csproj --nologo --verbosity minimal`
- Run one targeted test by name or fully qualified name:
  `dotnet test MyShop.Tests/MyShop.Tests.csproj --filter "Name~SomeTestName" --nologo --verbosity minimal`
  or
  `dotnet test MyShop.Tests/MyShop.Tests.csproj --filter "FullyQualifiedName~MyShop.Tests.SomeNamespace.SomeTestName" --nologo --verbosity minimal`
- Run a project directly (for local app validation):
  `dotnet run --project MyShop.Web/MyShop.Web.csproj`
  `dotnet run --project MyShop.Api/MyShop.Api.csproj`

There is no repo-defined lint script or custom analyzer task in the solution. Prefer `dotnet build` and `dotnet test` as the primary validation path. If formatting is needed, use the .NET formatter explicitly rather than inventing a project-specific lint workflow:

- `dotnet format MyShopSolution.sln --verify-no-changes`

## Architecture overview

This solution is organized into a layered application with a web storefront/admin portal and a JWT-based API:

- `MyShop.Web`: Blazor Server + Razor Pages application. This is the main UI and admin portal. It uses MudBlazor, session/cookie auth, optional Google sign-in, and multi-tenant DB wiring.
- `MyShop.Api`: ASP.NET Core API with JWT auth and Swagger. This project exposes business functionality to clients and uses the same application/infrastructure layers as the web app.
- `MyShop.Application`: Use-case/services layer; interfaces and service implementations for business logic and cross-cutting features.
- `MyShop.Core`: Domain models, core interfaces, shared security and tenant abstractions.
- `MyShop.Infrastructure`: Repository implementations and data access; includes SQL Server-backed distributed cache and tenant-aware connection logic.
- `MyShop.Shared`: Shared contracts and DTOs used across the solution.
- `MyShop.Tests`: xUnit test project covering the solution.

The repo also has a modular architecture: service registration is centralized using extension methods such as `AddUsersModule()`, `AddShopModule()`, and similar registration methods in `MyShop.Web/Extensions/ServiceCollectionModuleExtensions.cs`, rather than ad-hoc service wiring spread through startup code.

## Project-specific conventions and constraints

- Target framework: .NET 8 (`net8.0`).
- Authentication and config patterns are environment-specific:
  - `MyShop.Web` loads `appsettings.Local.json` and optional `modulesettings.json` before app startup.
  - Local secret values should stay out of source control; the repo docs explicitly recommend `dotnet user-secrets` for connection strings and DB credentials.
  - `MyShop.Web/docs/امنیت-اتصال-دیتابیس.md` is the source of truth for secret-handling guidance.
- Multi-tenant connection logic is active in both `MyShop.Web` and `MyShop.Api`; configuration keys such as `ConnectionStrings:DefaultConnection` / `ConnectionStrings:Default` and `ConnectionStrings:Tenants` are important when debugging startup or DB issues.
- Security is permission-driven: both web and API projects register a permission engine and SQL Server-backed distributed cache (`PermissionCacheKeys.ServiceKey`).
- The repo includes a local .NET tool manifest (`MyShop.Web/.config/dotnet-tools.json` and `MyShop.Api/.config/dotnet-tools.json`) with `dotnet-ef`; restore tools with `dotnet tool restore` if you need migrations or EF commands.
- There is no root README. Treat `MyShop.Web/docs/*.md` and this file as the repo’s operational documentation for platform-specific decisions.

## Useful repo references

- `MyShopSolution.sln` is the top-level entry point for solution-wide operations.
- `MyShop.Web/Program.cs` and `MyShop.Api/Program.cs` are the canonical startup files for app wiring and dependency registration.
- `MyShop.Web/Extensions/ServiceCollectionModuleExtensions.cs` is where module-level registrations live.
- `MyShop.Web/docs/امنیت-اتصال-دیتابیس.md` documents how local secrets and DB connection strings should be handled.

## Working style for this repo

- Prefer updates that match the existing layered architecture instead of introducing new entry points or bypassing the app’s service registration pattern.
- Keep environment-specific secrets in user secrets or per-machine config files, not committed JSON.
- For changes that affect startup/configuration or tenancy behavior, validate both the web app and API startup paths before finalizing the change.
- When implementing new features, use the existing module/service structure rather than bolting on one-off controllers or repositories in the wrong layer.
