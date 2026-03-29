# API and Validation Conventions

## API Layer Rules

- Controllers exist only to handle HTTP
- Controllers trigger validation via FluentValidation.
- Controllers must not contain manual validation logic.
- Controllers call application services
- Controllers return DTO responses with meaningful status codes
- Controllers do not access repositories or `DbContext` directly
- Controllers must inherit `AppControllerBase.cs`

## Controller Naming

- Singular resource name + `Controller`

## Endpoint Examples For This Project

- Follow **REST conventions**:
  - `GET /api/payments/{id}`
  - `GET /api/payments`
  - `POST /api/payments`
  - `GET /api/subscriptions/{id}`
  - `GET /api/subscriptions`
  - `POST /api/subscriptions`
  - `POST /api/subscriptions/{id}/cancel`

## Response Rules

- Use `201 Created` for successful create endpoints when appropriate
- Use `400` for validation failures
- Use `404` when resources are missing
- Use `409` for concurrency or conflict scenarios
- Do not expose internal implementation details in error responses

## Validation Rules

- Each request DTO has a corresponding FluentValidation validator where applicable
- Create DTOs in `Nexus.Application/Dtos`
- Add a validation class using FluentValidation in the same dto file
- Validators check required fields, ranges, and formats
- Validators do not access `DbContext` or repositories

## DTO Mapping

DTO mapping may occur in:

- application services: `Nexus.Application/Dtos`
- dedicated mapping helpers

Controllers should not perform complex mapping logic.
