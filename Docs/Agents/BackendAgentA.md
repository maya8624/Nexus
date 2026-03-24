# Backend Agent A Guide

## Purpose

Backend Agent A is responsible for delivering the backend implementation for the Inspection Booking feature within the existing Property module.

Use this file together with:

- `Docs/Booking/FeatureBooking.md`
- `Docs/Shared/ApiConventions.md`
- `Docs/Shared/CodingConventions.md`
- `Docs/Shared/Rules.md`

## Primary Responsibility

Own the backend application flow for inspection bookings across:

- `Nexus.Application`
- `Nexus.Infrastructure`
- `Nexus.Api`
- `Nexus.Tests`

This feature must extend the current `Property` flow. Do not create a parallel booking module unless the task explicitly requires it.

## Feature Scope

Implement support for:

- Create inspection booking request
- Cancel booking
- Get booking by id
- Check availability against overlapping `Confirmed` bookings

Bookings are requests first and should be created with `Pending` status.

## Existing Code To Extend

Prefer extending these existing files/components:

- `Nexus.Domain/Entities/InspectionBooking.cs`
- `Nexus.Domain/Enums/PropertyEnums.cs`
- `Nexus.Application/Interfaces/IPropertyService.cs`
- `Nexus.Application/Services/PropertyService.cs`
- `Nexus.Infrastructure/Interfaces/IPropertyRepository.cs`
- `Nexus.Infrastructure/Repositories/PropertyRepository.cs`
- `Nexus.Api/Controllers/PropertyController.cs`
- `Nexus.Application/Dtos`
- `Nexus.Tests/Application`

## API Expectations

Use `PropertyController` and follow the shared API conventions.

Expected endpoints:

- `POST /api/property/bookings`
- `POST /api/property/bookings/{id}/cancel`
- `GET /api/property/bookings/{id}`
- `GET /api/property/bookings/availability`

Controllers must:

- inherit from `AppControllerBase`
- stay thin
- validate request DTOs
- call application services only
- return meaningful HTTP status codes

## Application Layer Expectations

Add booking methods to `IPropertyService` and implement them in `PropertyService`.

Suggested methods:

- `CreateInspectionBookingAsync`
- `CancelInspectionBookingAsync`
- `GetInspectionBookingByIdAsync`
- `CheckInspectionAvailabilityAsync`

Application services must:

- keep business rules in the application layer
- accept and propagate `CancellationToken`
- use async methods
- use result-based responses for expected business failures
- avoid exceptions for normal validation or business-rule flow
- use the shared `Result<T>` type for expected business failures

## Repository Expectations

Extend `IPropertyRepository` and `PropertyRepository` for persistence and validation queries required by booking flows.

Repository responsibilities may include:

- loading property and listing validation data
- finding a booking by id
- checking overlapping confirmed bookings
- checking duplicate booking requests
- saving a new booking

Do not place business rules in the repository.

## Validation Rules

Create DTOs and FluentValidation validators in `Nexus.Application/Dtos`.

Minimum validation requirements:

- required ids must not be empty
- `UserId` is included in the request body for now
- `InspectionStartAtUtc` and `InspectionEndAtUtc` are required
- `InspectionStartAtUtc < InspectionEndAtUtc`
- booking cannot be created in the past
- `Notes` length must align with entity configuration

## Business Rules

- all times are stored in UTC
- property must exist and be active
- `ListingId` is required and must belong to the property and be active/published
- overlapping `Confirmed` bookings are not allowed
- overlapping `Pending` bookings are allowed
- duplicate requests from the same user for the same property and same time window are not allowed
- only `Pending` or `Confirmed` bookings can be cancelled

## Testing Requirements

Add or update tests in `Nexus.Tests/Application/PropertyServiceTests.cs`.

Minimum test coverage:

- create booking succeeds for valid request
- create booking fails when property does not exist
- create booking fails when listing is invalid
- create booking fails when the request is in the past
- create booking fails when the time range is invalid
- create booking fails for duplicate request
- availability returns unavailable for overlapping confirmed bookings
- availability ignores pending bookings
- cancel booking succeeds from pending
- cancel booking fails for invalid status transitions

## Working Style

- preserve existing architecture and naming patterns
- prefer focused changes over broad refactors
- update existing files when they are already the right extension point
- add migrations only if schema changes are required
- keep comments minimal and useful

## Notes For Agent A

- The repository already contains `InspectionBooking` and EF Core configuration, so start from what exists.
- Treat `InspectionEndAtUtc` as required even though the current entity allows null.
- Keep `UserId` in the request body for now, even though it should come from auth context later.
- Require `ListingId` for create and availability checks.
- Use `POST /{id}/cancel` instead of a custom HTTP verb.
- Return `200 OK` with payload for cancellation.
- Availability should be blocked only by `Confirmed` bookings.
