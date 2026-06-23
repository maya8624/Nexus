# Nexus

A real estate property management and transaction platform built with .NET 8 and PostgreSQL. Handles property listings, agent/agency profiles, inspection bookings, deposits, and AI-powered chat.

## Tech Stack

- **Runtime:** .NET 8 / ASP.NET Core Web API
- **Database:** PostgreSQL (via EF Core + Npgsql, snake_case naming)
- **Auth:** JWT Bearer + Google OAuth
- **Payments:** Stripe (deposits), PayPal
- **Validation:** FluentValidation
- **Email:** MailKit (SMTP)
- **Background Jobs:** Hangfire (PostgreSQL-backed, with dashboard at `/hangfire`)
- **Logging:** Serilog (console + daily rolling file under `logs/`)
- **AI Chat:** External sidecar service (SSE streaming)
- **File Storage:** Azure Blob Storage (SAS URL upload flow)
- **Event Processing:** Azure Functions (isolated worker, blob trigger)
- **Observability:** Application Insights (Functions)
- **Secrets:** Azure Key Vault
- **Docs:** Swagger / Swashbuckle
- **Hosting:** Azure App Service (Web API) + Azure Functions
- **Database hosting:** Azure Database for PostgreSQL
- **CI/CD:** GitHub Actions

## Project Structure

```
Nexus/
├── Nexus.Api/             # Controllers, middleware, DI extensions
├── Nexus.Application/     # Services, DTOs, interfaces, validators
├── Nexus.Domain/          # Entities and enums
├── Nexus.Functions/       # Azure Functions (isolated worker — blob ingestion trigger)
├── Nexus.Infrastructure/  # EF Core DbContext, repositories, migrations
├── Nexus.Network/         # External HTTP clients (PayPal, AI service)
└── Nexus.Tests/           # Test project
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 12+ (dev default: `localhost:5433`, database: `real_estate_db`)
- Stripe test account and webhook secret
- Google OAuth client credentials
- AI sidecar service running at `http://localhost:8000` (development)

## Getting Started

**1. Configure secrets**

Copy `appsettings.Development.json` and fill in the required values (connection string, JWT key, Stripe keys, Google OAuth credentials). In production, secrets are loaded automatically from **Azure Key Vault** — set `KeyVaultUrl` in the App Service application settings.

**2. Apply migrations**

```bash
dotnet ef database update --project Nexus.Infrastructure --startup-project Nexus.Api
```

**3. Run the API**

```bash
dotnet run --project Nexus.Api
```

Swagger UI is available at `https://localhost:7289/swagger` in development.

## Key Features

| Domain | Details |
|---|---|
| **Properties** | Houses, apartments, townhouses, villas, land — with addresses, images, bedrooms/bathrooms/car spaces |
| **Listings** | Sale or rent; statuses: Draft → Active → Under Offer → Sold/Leased/Withdrawn |
| **Agents & Agencies** | Profiles with licence numbers, bios, photos; linked to properties and listings |
| **Inspection Bookings** | Agent-created slots; user bookings with status tracking (Pending, Confirmed, Cancelled, Completed, No Show) |
| **Deposits** | Stripe checkout sessions with webhook confirmation; idempotency keys; multi-currency |
| **PayPal** | Sandbox order creation, capture, and refunds |
| **AI Chat** | Per-user chat sessions with streaming (Server-Sent Events); tool execution tracking |
| **AI Features** | Tenant preference property search, suburb market summaries, AI-generated enquiry reply drafts |
| **File Uploads** | SAS URL flow — client uploads directly to Azure Blob Storage; server issues time-limited SAS tokens and tracks upload status; upload purposes: General, Extraction, Ingestion, Invoice |
| **Document Ingestion** | Blob trigger (Azure Function) fires on new uploads; calls internal API to forward content to the AI sidecar for indexing |
| **Invoice Extraction** | AI-powered extraction of structured invoice data (vendor, line items, totals, dates) from uploaded documents; results persisted as `Invoice` records linked to the source file upload and optionally a property |
| **Enquiries** | Tenants submit enquiries to agents; agents draft and send replies; outgoing reply emails are dispatched via a Hangfire background job |
| **Auth** | Email/password registration + login; Google external login; JWT (60-min expiry) + rotating refresh tokens (7-day expiry) |

## API Overview

| Controller | Responsibilities |
|---|---|
| `AuthController` | Register, login, external login (Google), token refresh, `/me` |
| `PropertyController` | List/filter properties, get by ID |
| `DepositsController` | Stripe checkout, Stripe webhook |
| `InspectionSlotController` | Create and manage inspection time slots |
| `InspectionBookingController` | Book and manage inspection appointments |
| `EnquiryController` | Submit, update, and send replies to property enquiries |
| `AiController` | Chat, streaming chat, preference search, suburb summaries, enquiry draft generation, document ingestion |
| `FilesController` | Generate SAS upload URLs, confirm completed uploads |
| `InvoiceController` | Get extracted invoice by file upload ID, update invoice fields |
| `PayPalController` | PayPal payment flow |
| `OrderController` | Legacy order management |
| `InternalController` | Admin/internal operations (API-key protected): inspection bookings, deposits, document ingestion, invoice extraction |

## Authentication

Protected endpoints require a JWT in the `Authorization: Bearer <token>` header. Tokens are issued on login/register and expire after 60 minutes (configurable).

**Refresh tokens** are also issued on login/register and expire after 7 days. Call `POST /api/auth/refresh` with the refresh token to receive a new access token and a new refresh token (single-use rotation — the old token is revoked immediately). If the refresh token is expired or already used, re-authenticate via login.

The `IUserContext` abstraction resolves the current user inside services — user IDs are **not** passed as method parameters.

## Payments

**Stripe** is the primary payment provider for property deposits. The flow is:

1. `POST /api/deposits/checkout` — creates a Stripe Checkout Session
2. Stripe redirects the user back; webhook fires `checkout.session.completed`
3. `POST /api/deposits/webhook` — verifies the signature and marks the deposit as paid

**PayPal** is available as an alternative (sandbox only in development).

## Environment Configuration

| Key section | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `JwtSettings` | Issuer, audience, signing key, access token expiry (`ExpiryMinutes`), refresh token expiry (`RefreshTokenExpiryDays`) |
| `GoogleAuth` | OAuth client ID and secret |
| `StripeSettings` | Secret key and webhook signing secret |
| `PayPalSettings` | Client ID/secret, OAuth and order endpoints |
| `AiServiceSettings` | Base URL, API key, and endpoint paths for the AI sidecar (chat, stream, preferences, suburb-summary, enquiry-draft, ingestion, invoice-extract) |
| `BlobStorageSettings` | Azure Blob Storage connection string, container names (general, extraction, ingestion, invoice), SAS token expiry (minutes) |
| `SmtpSettings` | Outlook SMTP host, port, credentials |
| `CorsSettings:AllowedOrigins` | Frontend origins (e.g. `http://localhost:5173`) |
| `KeyVaultUrl` | Azure Key Vault URI (production only) |
| `Serilog` | Log level overrides per environment (see `appsettings.json`) |

## Deployment

The API is hosted on **Azure App Service**. The database runs on **Azure Database for PostgreSQL (Flexible Server)**. All production secrets (connection string, JWT key, Stripe keys, etc.) are stored in **Azure Key Vault** and loaded at startup via the `KeyVaultUrl` configuration key.

Deployments are automated via **GitHub Actions**. Pushing to `main` triggers the workflow which builds, tests, and deploys both the API (Azure App Service) and the Functions app (`func-rec-ingest-dev`).

No secrets should ever be committed to source control. The `appsettings.json` in the repository contains only empty placeholders.

## Development Notes

- Frontend dev servers run on `http://localhost:5173` and `http://localhost:5174`.
- The AI sidecar (`rec_brain`) runs on `http://localhost:8000` and requires an `X-API-Key` header. It exposes endpoints for chat, streaming chat, preference search, suburb summaries, enquiry draft generation, and document ingestion — all configured under `AiServiceSettings`.
- Email notifications for inspection booking confirmations/cancellations are not yet implemented.
- SMTP is configured via `SmtpSettings` (MailKit). Enquiry reply emails are sent as Hangfire background jobs.
- The Hangfire dashboard is available at `/hangfire` and requires a valid JWT Bearer token.
- Logs are written to console and `logs/nexus-YYYYMMDD.log` (daily rollover). Log levels are controlled per environment via the `Serilog` config section.
