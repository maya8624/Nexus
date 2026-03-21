# Nexus Backend Project Summary

## Current Active Backend

The active .NET backend is the `Nexus.sln` solution at the repository root. It currently includes these projects:

- `Nexus.Api`
- `Nexus.Application`
- `Nexus.Domain`
- `Nexus.Infrastructure`
- `Nexus.Network`
- `Nexus.Tests`

There are also `NexusPay.*` folders in the repo root, but they are not referenced by `Nexus.sln` and currently only contain `obj` output. `Nexus.Configuration` is also present at the root but is not part of the active solution and contains only build artifacts.

## High-Level Architecture

The backend follows a layered structure:

- `Nexus.Api`: ASP.NET Core Web API entry point, controllers, auth setup, middleware, Swagger, and DI/bootstrap code.
- `Nexus.Application`: service layer, DTOs, validators, auth/token logic, business workflows, and application-facing interfaces.
- `Nexus.Domain`: core entities and enums.
- `Nexus.Infrastructure`: EF Core `AppDbContext`, entity configurations, migrations, repositories, and unit of work.
- `Nexus.Network`: outbound HTTP integration helpers and PayPal-specific network/auth support.
- `Nexus.Tests`: xUnit/Moq tests for middleware, application services, and network helpers.

## Runtime Setup

`Nexus.Api/Program.cs` configures the application as follows:

- Registers infrastructure and application services.
- Adds controllers and JSON enum serialization.
- Enables JWT bearer authentication.
- Reads JWT from an HTTP cookie during bearer token validation.
- Enables Swagger in development.
- Uses global exception middleware.
- Uses CORS, HTTPS redirection, authentication, authorization, and controller mapping.

The current API project targets `.NET 8`.

## Current API Controllers

Controllers live in `Nexus.Api/Controllers` and inherit from `AppControllerBase`, which applies:

- `[Authorize]`
- `[ApiController]`
- `[Route("api/[controller]")]`

Current controllers:

- `AuthController`
  - Email login/register
  - External login
  - Logout
  - Current-user lookup (`me`)
- `OrderController`
  - Get order by id
  - Get orders for current user
  - Create order
  - Delete order
- `PayPalController`
  - Create PayPal order
  - Capture PayPal order
  - Refund capture
  - Placeholder webhook endpoint
- `AiController`
  - Chat endpoint backed by an AI service
  - Marked `[AllowAnonymous]`
- `PropertyController`
  - Exists but is currently empty

## Important Observation

The repository currently mixes two backend domains:

- A payment flow: orders, payments, refunds, PayPal, AI sidecar/fraud-related DTOs.
- A property/real-estate flow: properties, agencies, agents, listings, enquiries, inspection bookings, saved properties, chat sessions/messages.

This is visible in the `Domain` and `Infrastructure` layers, while the API surface is still mostly payment-oriented. `PropertyController` appears to be the entry point intended for future property endpoints, but no endpoints are implemented yet.

## Project-by-Project Structure

### 1. `Nexus.Api`

Purpose: host the HTTP API and wire the application together.

Key files:

- `Program.cs`
- `appsettings.json`
- `appsettings.Development.json`
- `Controllers/AppControllerBase.cs`
- `Controllers/AuthController.cs`
- `Controllers/OrderController.cs`
- `Controllers/PayPalController.cs`
- `Controllers/AiController.cs`
- `Controllers/PropertyController.cs`
- `Extensions/AuthExtensions.cs`
- `Extensions/InfrasExtensions.cs`
- `Middleware/ExceptionHandlingMiddleware.cs`

Main responsibilities:

- Application startup and DI bootstrap
- JWT auth setup
- Swagger/OpenAPI config
- CORS setup
- Request pipeline and exception handling
- Public API endpoints

### 2. `Nexus.Application`

Purpose: hold business logic and application contracts.

Main folders:

- `Constants`
- `Dtos`
- `Exceptions`
- `Extensions`
- `Factories`
- `Interfaces`
- `Services`
- `Settings`

Notable interfaces:

- `IAiService`
- `IAuthService`
- `IOrderService`
- `IPaymentService`
- `IPayPalService`
- `ITokenService`
- `IUserService`
- `IFraudDetectionService`

Notable services:

- `AiService`
- `OrderService`
- `PaymentServcie`
- `PayPalService`
- `UserService`
- `TokenService`
- `FraudDetectionService`
- `AuthEmailService`
- `AuthGoogleService`
- `AuthMicroSoftService`
- `PasswordHasherService`

Other notable pieces:

- DTOs for auth, orders, PayPal, refunds, AI chat, and fraud requests/responses
- `AuthServiceFactory` for provider-based auth selection
- settings classes for JWT/auth/AI configuration

Note: service registration is currently implemented in `Nexus.Api/Extensions/AppExtensions.cs`, even though the namespace is `Nexus.Application.Extensions`.

### 3. `Nexus.Domain`

Purpose: define the core data model and shared enums.

Entities:

- Payment-related: `Order`, `OrderItem`, `Payment`, `Refund`
- Property-related: `Property`, `PropertyAddress`, `PropertyImage`, `PropertyType`, `Listing`, `SavedProperty`
- Agency/user-related: `Agency`, `Agent`, `User`, `UserLogin`, `Enquiry`, `InspectionBooking`
- AI/support-related: `ChatSession`, `ChatMessage`, `ToolExecution`

Enums:

- Payment: `Currency`, `OrderStatus`, `PaymentProvider`, `PaymentStatus`, `RefundStatus`
- Property/user: `PropertyEnums`, `UserRole`

### 4. `Nexus.Infrastructure`

Purpose: persistence and database access.

Main folders:

- `Interfaces`
- `Persistence`
- `Repositories`
- `Responses`
- `Migrations`

Key files:

- `Persistence/AppDbContext.cs`
- `UnitOfWork.cs`
- repository interfaces and implementations
- EF Core entity configuration classes under `Persistence/Configurations`

`AppDbContext` currently exposes `DbSet`s for:

- Payment objects: orders, order items, payments
- Property/real-estate objects: agencies, agents, listings, properties, addresses, images, saved properties, enquiries, inspection bookings
- User and AI-related objects: users, logins, chat sessions/messages, tool executions

Database notes:

- The app is configured to use PostgreSQL via `UseNpgsql(...)`
- `UseSnakeCaseNamingConvention()` is enabled
- Migrations already exist under `Nexus.Infrastructure/Migrations`
- There is commented-out SQL Server setup, suggesting an earlier provider change

### 5. `Nexus.Network`

Purpose: encapsulate outbound HTTP and PayPal network integration support.

Main contents:

- `HttpClientService`
- `PayPalAuthService`
- `HttpRequestFactory`
- `HttpStatusFailureMap`
- `PayPalSettings`
- network-specific exceptions and response types

This layer appears to support external API communication, especially PayPal auth/token acquisition and HTTP failure mapping.

### 6. `Nexus.Tests`

Purpose: cover unit tests across API/application/network behavior.

Current test files:

- `Api/ExceptionHandlingMiddlewareTests.cs`
- `Application/AuthFactoryTests.cs`
- `Application/OrderServiceTests.cs`
- `Application/UserServiceTests.cs`
- `Network/HttpStatusFailureMapTests.cs`

This indicates basic test coverage exists for middleware, auth factory behavior, order logic, user logic, and network error mapping.

## Current Root Structure

Active and notable root folders:

```text
Nexus/
+-- Nexus.sln
+-- Nexus.Api/
+-- Nexus.Application/
+-- Nexus.Domain/
+-- Nexus.Infrastructure/
+-- Nexus.Network/
+-- Nexus.Tests/
+-- Nexus.Configuration/        # not active; only build artifacts at present
+-- NexusPay.Api/               # not in solution
+-- NexusPay.Application/       # not in solution
+-- NexusPay.Domain/            # not in solution
+-- NexusPay.Infrastructure/    # not in solution
+-- NexusPay.Network/           # not in solution
+-- README.md
```

## Current State Summary

The backend is best described as a layered `.NET 8` ASP.NET Core API with:

- working payment-related endpoints and services,
- authentication/token plumbing,
- AI chat integration,
- a larger property/real-estate data model already added to the domain and persistence layers,
- but no implemented property endpoints yet.

For the next task, `PropertyController` is the clear place to add new property-facing endpoints, and the existing domain/infrastructure model already provides a base to build on.
