# Inspection Booking Feature Guide

## Goal

Implement an inspection booking workflow that allows users to:

- Request a property inspection
- Cancel an existing booking request
- View booking details and status
- Check whether a requested inspection window conflicts with confirmed bookings

Bookings are requests first. They are not instant confirmations.

## Current Codebase Notes

The repository already contains these booking foundations:

- Domain entity: `Nexus.Domain/Entities/InspectionBooking.cs`
- Enum: `Nexus.Domain/Enums/PropertyEnums.cs`
- EF Core configuration: `Nexus.Infrastructure/Persistence/Configurations/InspectionBookingConfiguration.cs`
- `InspectionBooking.Status` is stored as a string in the database
- `InspectionEndAtUtc` is nullable in the current entity, so application logic must decide

This feature should extend the existing Property flow instead of introducing a parallel service or repository stack.

## Scope

### Included

- Create inspection booking with `Pending` status
- Cancel booking
- Get booking by id
- Basic availability check based on overlapping `Confirmed` bookings
- Prevent duplicate requests from the same user for the same property and time window
- Confirmed bookings may already exist from seeded data, admin/manual updates, or future workflows outside this feature.

### Not Included

- Predefined inspection time slots
- Calendar sync
- Notifications
- Payments
- Agent-side confirm/reject workflow
- Idempotency keys

## Domain Model

### Entity

Use the existing `InspectionBooking` entity in `Nexus.Domain/Entities`.

Current fields:

- `Id`
- `UserId`
- `PropertyId`
- `ListingId`
- `AgentId`
- `InspectionStartAtUtc`
- `InspectionEndAtUtc`
- `Status`
- `Notes`
- `CreatedAtUtc`
- `UpdatedAtUtc`

### Status Enum

Use the existing enum from `Nexus.Domain.Enums`:

```csharp
public enum InspectionBookingStatus
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3,
    Completed = 4,
    NoShow = 5
}
```

Note:

- The current enum does not include `Rejected`
- If rejection is needed later, add it deliberately with migration and transition updates

## API Design

Follow the repository conventions:

- Controller: `PropertyController`
- Base class: `AppControllerBase`
- Controllers stay thin
- Request DTOs and validators live in `Nexus.Application/Dtos`
- Services own business rules

### Endpoints

#### Create Booking

- `POST /api/property/bookings`

Request body:

- `ListingId` required
- `PropertyId` required
- `AgentId` optional
- `UserId` required in the request body for now
- `InspectionStartAtUtc` required
- `InspectionEndAtUtc` required
- `Notes` optional

Authentication note:

- `UserId` should eventually come from the authenticated user context
- For now, include `UserId` in the request body so implementation can proceed without waiting on auth integration

Behavior:

- Validate property exists and is active
- Validate listing exists, belongs to the property, and is active/published
- Validate `InspectionStartAtUtc < InspectionEndAtUtc`
- Reject requests in the past
- Prevent duplicate booking requests by the same user for the same property and same time window
- Create booking with `Pending` status
- Set `CreatedAtUtc` and `UpdatedAtUtc`

Response:

- `201 Created`
- Return created booking DTO
- Prefer `CreatedAtAction` to the get-by-id endpoint

Suggested response DTO:

```csharp
public sealed class InspectionBookingDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid PropertyId { get; init; }
    public Guid ListingId { get; init; }
    public Guid? AgentId { get; init; }
    public DateTimeOffset InspectionStartAtUtc { get; init; }
    public DateTimeOffset InspectionEndAtUtc { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
}
```

#### Cancel Booking

- `POST /api/property/bookings/{id}/cancel`

Reason:

- This matches the existing API conventions better than a custom `CANCEL` verb

Behavior:

- Only `Pending` or `Confirmed` bookings can be cancelled
- Update `Status` to `Cancelled`
- Update `UpdatedAtUtc`

Response:

- `200 OK` with updated booking DTO
- `404` if booking does not exist
- `409` if current status does not allow cancellation

#### Get Booking

- `GET /api/property/bookings/{id}`

Response:

- Booking details
- Current status

#### Check Availability

- `GET /api/property/bookings/availability`

Query string:

- `propertyId` required
- `inspectionStartAtUtc` required
- `inspectionEndAtUtc` required
- `listingId` required
- `excludeBookingId` optional for future edit scenarios

Behavior:

- Validate time window
- Check overlap only against `Confirmed` bookings
- `Pending` bookings do not block availability

Response:

- `200 OK`
- `isAvailable`
- `message`

Suggested response DTO:

```csharp
public sealed class InspectionAvailabilityResponse
{
    public bool IsAvailable { get; init; }
    public string Message { get; init; } = string.Empty;
}
```

## Business Rules

- Store all times in UTC
- `InspectionStartAtUtc` must be earlier than `InspectionEndAtUtc`
- Booking cannot be created in the past
- Property must exist and be active
- Listing must be provided, belong to the property, and be available for booking
- Overlapping `Confirmed` bookings are not allowed
- Overlapping `Pending` bookings are allowed
- Duplicate requests from the same user for the same property and same time window are not allowed
- Duplicate = same UserId + PropertyId + InspectionStartAtUtc + InspectionEndAtUtc
- Overlap = requestedStart < existingEnd && requestedEnd > existingStart
- Cancellation is allowed only from `Pending` or `Confirmed`

## Status Transitions

### Allowed

- `Pending -> Confirmed`
- `Pending -> Cancelled`
- `Confirmed -> Cancelled`
- `Confirmed -> Completed`
- `Confirmed -> NoShow`

### Not Allowed

- `Cancelled -> any`
- `Completed -> any`
- `NoShow -> any`

## Repository and Service Guidance

### Service

Extend `IPropertyService` and `PropertyService` with booking methods such as:

- `CreateInspectionBookingAsync`
- `CancelInspectionBookingAsync`
- `GetInspectionBookingByIdAsync`
- `CheckInspectionAvailabilityAsync`

All new application methods should:

- Be async
- Accept `CancellationToken`
- Return a result shape that supports business failures without relying on exceptions for expected flow
- Use `Result<T>` for expected business failures; do not throw exceptions for normal validation/business-rule flow
- Create a shared `Result<T>` type if the project does not already have one

### Repository

Extend `IPropertyRepository` and `PropertyRepository` with booking-focused persistence methods instead of creating a separate booking repository.

Suggested capabilities:

- Get property and listing validation data
- Get booking by id
- Detect overlapping confirmed bookings
- Detect duplicate user booking requests
- Add booking
- Save changes through the existing unit-of-work pattern used by the project

## Validation

Create request/response DTOs in `Nexus.Application/Dtos` and add FluentValidation validators in the same file.

Suggested request DTOs to create first:

```csharp
public sealed class CreateInspectionBookingRequest
{
    public Guid UserId { get; init; }
    public Guid PropertyId { get; init; }
    public Guid ListingId { get; init; }
    public Guid? AgentId { get; init; }
    public DateTimeOffset InspectionStartAtUtc { get; init; }
    public DateTimeOffset InspectionEndAtUtc { get; init; }
    public string? Notes { get; init; }
}

public sealed class CheckInspectionAvailabilityRequest
{
    public Guid PropertyId { get; init; }
    public Guid ListingId { get; init; }
    public DateTimeOffset InspectionStartAtUtc { get; init; }
    public DateTimeOffset InspectionEndAtUtc { get; init; }
    public Guid? ExcludeBookingId { get; init; }
}
```

Minimum validation rules:

- Required ids are not empty
- `InspectionStartAtUtc` and `InspectionEndAtUtc` are present
- `InspectionStartAtUtc < InspectionEndAtUtc`
- `InspectionStartAtUtc` is in the future
- `Notes` max length aligns with entity configuration

## Testing

Add unit tests for `PropertyService` in `Nexus.Tests/Application/PropertyServiceTests.cs`.

Cover at least:

- Create booking succeeds for valid request
- Create booking fails when property does not exist
- Create booking fails when listing is invalid for the property
- Create booking fails when time range is invalid
- Create booking fails when request is in the past
- Create booking fails for duplicate user request
- Availability returns unavailable when a confirmed booking overlaps
- Availability ignores pending bookings
- Cancel booking succeeds for `Pending`
- Cancel booking fails for already cancelled booking

## Implementation Suggestions

Before coding, I recommend these decisions:

1. Treat `InspectionEndAtUtc` as required even though the entity allows null today.
2. Use `POST /{id}/cancel` instead of a custom HTTP verb.
3. Keep availability based on `Confirmed` bookings only, otherwise users will be blocked by unreviewed requests.
4. Keep `UserId` in the request body for now, then move it to auth context later.
5. Require `ListingId` instead of treating it as optional.
6. Return `200 OK` with payload for cancellation.
7. Create the booking DTOs first so they can be reviewed and refined before wiring the full flow.
8. Keep booking logic in `PropertyService` for now to match the current architecture, then extract later only if the feature grows significantly.
9. Add repository queries that project only the fields needed for validation and conflict checks instead of loading full aggregates.
