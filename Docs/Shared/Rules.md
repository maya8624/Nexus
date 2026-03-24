# Architecture Rules

This file defines the mandatory architecture rules for the repository.

## Layer Boundaries

### Domain

- Contains entities, value objects, enums, and domain logic only
- Must not reference Application, Infrastructure, or API
- Must not depend on EF Core, logging frameworks, or other external libraries

### Application

- Contains business logic, DTOs, and interface contracts
- May reference Domain only
- Must depend on repository and unit of work abstractions
- Must not reference Infrastructure directly

### Infrastructure

- Implements persistence, integrations, repositories, and unit of work
- May reference Domain and Application abstractions
- Must not contain business rules that belong in Application or Domain

### API

- Handles HTTP, middleware, DI wiring, and response shaping
- May call Application services and use Infrastructure for DI wiring only
- Must not contain business logic or direct `DbContext` access

## Persistence Rules

This project uses EF Core Code-First.

- Domain entities define the source model
- Infrastructure configurations map the domain model
- Database schema changes must be applied through EF Core migrations
- Database-first scaffolding is not allowed
- Concurrency must be handled with `RowVersion` where required

## API Rules

- Controllers must remain thin
- Controllers validate inputs and call application services
- Controllers return DTO responses and correct HTTP status codes
- Controllers must not access repositories, `DbContext`, or Stripe SDK directly

## Validation Rules

- Every request DTO should have a validator where applicable
- Validators must be registered through DI
- Validators must not access repositories or `DbContext`

## Quality Rules

- Use async EF Core calls only
- Register dependencies in `Program.cs`
- Do not hardcode secrets
- Do not log secrets or sensitive values
