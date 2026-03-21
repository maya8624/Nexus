# Property Endpoints Implementation Guide

## Goal

Add read-only property endpoints through:

- `PropertyController`
- `IPropertyService`
- `PropertyService`
- property repository

Requested endpoints:

- `GET /api/property?page={n}&pageSize={n}&type={type?}`
- `GET /api/property/{id}`

## Important Fit With Current Codebase

Before implementation, there are two important mismatches with the current structure:

1. ID type mismatch

- Your DTO example uses `int Id`
- The current domain entity `Property` uses `Guid Id`
- The endpoint and DTO should follow the existing domain model, otherwise the implementation will fight the current schema and EF mappings

Recommended adjustment:

```csharp
public Guid Id { get; init; }
```

If you want to keep `int` in the DTO, that would require a separate external identifier strategy, which does not exist in the current model.

## Endpoints To Build

### 1. Get Paged Property List

Route:

```http
GET /api/property?page={n}&pageSize={n}&type={type?}
```

Query parameters:

- `page`: default `1`, minimum `1`
- `pageSize`: default `20`, minimum `1`, cap at `100`
- `type`: optional, valid values:
  - `House`
  - `Apartment`
  - `Townhouse`
  - `Villa`
  - `Land`

Expected behavior:

- Return paged properties
- Filter by property type when `type` is provided
- Only return active/published property records
- Sort consistently, preferably newest listed first

### 2. Get Property By Id

Route:

```http
GET /api/property/{id}
```

Recommended route parameter type:

```csharp
Guid id
```

Expected behavior:

- Return a single property detail DTO
- Return `404` when not found

## DTOs To Add

Create these DTOs under `Nexus.Application/Dtos`.

Recommended version aligned to the current entity model:

```csharp
public sealed class AgentDto
{
    public string Name { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Agency { get; init; } = string.Empty;
    public string Photo { get; init; } = string.Empty;
}

public sealed class PropertyDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string Suburb { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Postcode { get; init; } = string.Empty;
    public string Price { get; init; } = string.Empty;
    public decimal PriceValue { get; init; }
    public string PropertyType { get; init; } = string.Empty;
    public int Bedrooms { get; init; }
    public int Bathrooms { get; init; }
    public int Parking { get; init; }
    public int LandSize { get; init; }
    public string Description { get; init; } = string.Empty;
    public string[] Features { get; init; } = Array.Empty<string>();
    public string[] Images { get; init; } = Array.Empty<string>();
    public AgentDto Agent { get; init; } = new();
    public string? AuctionDate { get; init; }
    public bool IsNew { get; init; }
    public bool IsFeatured { get; init; }
    public string[] InspectionTimes { get; init; } = Array.Empty<string>();
    public string ListedDate { get; init; } = string.Empty;
}
```

## Additional DTOs Recommended

To support paging cleanly, add a paged response DTO instead of returning a raw array.

Recommended:

```csharp
public sealed class PropertyListResponse
{
    public IReadOnlyList<PropertyDto> Items { get; init; } = Array.Empty<PropertyDto>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}
```

Also add a query DTO so validation and defaults stay out of the controller.

```csharp
public sealed class PropertyQueryRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Type { get; init; }
}
```

## Service Layer Design

### `IPropertyService`

Add a new interface under `Nexus.Application/Interfaces`.

Recommended contract:

```csharp
public interface IPropertyService
{
    Task<PropertyListResponse> GetProperties(int page, int pageSize, string? type, CancellationToken ct);
    Task<PropertyDto?> GetPropertyById(Guid id, CancellationToken ct);
}
```

Responsibilities:

- validate and normalize query inputs
- parse `type` into the existing `PropertyType` enum
- call repository methods
- map database entities/projections into `PropertyDto`
- return `null` for missing property detail

### `PropertyService`

Add a new service under `Nexus.Application/Services`.

Recommended responsibilities:

- enforce defaults:
  - page = `1` when invalid
  - pageSize = `20` when invalid
  - pageSize max = `100`
- validate `type`
- use repository projection methods
- format values for API output:
  - `Price`
  - `ListedDate`
  - `AuctionDate`
- build `AgentDto`
- flatten address and image collections

Important mapping notes from current schema:

- `Property.Title` -> `PropertyDto.Title`
- `Property.Address.AddressLine1` -> `PropertyDto.Address`
- `Property.Address.Suburb/State/Postcode` -> matching DTO fields
- `Property.Bedrooms` -> `Bedrooms`
- `Property.Bathrooms` -> `Bathrooms`
- `Property.CarSpaces` -> `Parking`
- `Property.LandSizeSqm` -> `LandSize`
- `Property.Description` -> `Description`
- `Property.Images` -> `Images`
- `Property.Agent.FirstName + LastName` -> `Agent.Name`
- `Property.Agent.PhoneNumber` -> `Agent.Phone`
- `Property.Agent.PhotoUrl` -> `Agent.Photo`
- `Property.Agency.Name` -> `Agent.Agency`

Fields that are not fully supported by the current schema should be mapped deliberately:

- `Features`: there is no obvious property-features table in the current model, so return an empty array for now
- `InspectionTimes`: no direct public display field exists; return an empty array for now unless you derive it from `InspectionBooking`
- `IsFeatured`: no direct field exists on `Property` or `Listing`; default to `false` unless you add a new column
- `AuctionDate`: only populate if the listing model is extended to support auction metadata; otherwise `null`
- `IsNew`: can be derived from `ListedAtUtc`, for example within the last 14 days

## Repository Layer Design

The current repository base is centered on `int` IDs and generic CRUD, so property queries should use a dedicated repository rather than force-fitting into the existing base methods.

### Add `IPropertyRepository`

Add under `Nexus.Infrastructure/Interfaces`.

Recommended contract:

```csharp
public interface IPropertyRepository
{
    Task<(IReadOnlyList<Property> Items, int TotalCount)> GetPagedProperties(
        int page,
        int pageSize,
        Domain.Enums.PropertyType? type,
        CancellationToken ct);

    Task<Property?> GetPropertyById(Guid id, CancellationToken ct);
}
```

### Add `PropertyRepository`

Add under `Nexus.Infrastructure/Repositories`.

Recommended query behavior:

- start from `_context.Properties`
- include:
  - `Address`
  - `Images`
  - `Agent`
  - `Agency`
  - `PropertyType`
  - `Listings`
- filter active properties
- for list endpoint, prefer active and published listings
- use `AsNoTracking()`
- sort by latest listing date descending
- apply paging with `Skip` and `Take`
- return total count before paging

Suggested list query shape:

```csharp
_context.Properties
    .AsNoTracking()
    .Include(x => x.Address)
    .Include(x => x.Images)
    .Include(x => x.Agent)
    .Include(x => x.Agency)
    .Include(x => x.PropertyType)
    .Include(x => x.Listings)
    .Where(x => x.IsActive)
```

Type filtering:

- The incoming `type` should be parsed against `Nexus.Domain.Enums.PropertyType`
- The actual entity stores `PropertyTypeId` and has a `PropertyType` navigation
- Filtering can be done using either:
  - `x.PropertyTypeId == (int)parsedType`
  - or `x.PropertyType.Name == type`

Prefer `PropertyTypeId` because it aligns better with the current entity structure.

## Controller Design

### `PropertyController`

The controller should stay thin and delegate to the service.

Recommended shape:

```csharp
public class PropertyController : AppControllerBase
{
    private readonly IPropertyService _propertyService;

    public PropertyController(IPropertyService propertyService)
    {
        _propertyService = propertyService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PropertyListResponse>> GetProperties(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? type = null,
        CancellationToken ct = default)
    {
        var result = await _propertyService.GetProperties(page, pageSize, type, ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PropertyDto>> GetProperty(Guid id, CancellationToken ct)
    {
        var result = await _propertyService.GetPropertyById(id, ct);

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
```

## Dependency Injection Updates

Register the new property components in the same place current services are wired:

- `Nexus.Api/Extensions/AppExtensions.cs`

Add:

```csharp
services.AddScoped<IPropertyService, PropertyService>();
services.AddScoped<IPropertyRepository, PropertyRepository>();
```

## Validation Rules

Recommended controller/service validation behavior:

- if `page < 1`, use `1`
- if `pageSize < 1`, use `20`
- if `pageSize > 100`, clamp to `100`
- if `type` is present but invalid, return `400 Bad Request`

Valid `type` values should match:

- `House`
- `Apartment`
- `Townhouse`
- `Villa`
- `Land`

These already exist in `Nexus.Domain.Enums.PropertyType`.

## Response Mapping Notes

### Price

The current model stores `Listing.Price` as `decimal`.

Recommended mapping:

- `PriceValue` = raw decimal price
- `Price` = formatted string, for example:

```csharp
price.ToString("C0", CultureInfo.GetCultureInfo("en-AU"))
```

### Images

Use ordered image URLs:

```csharp
property.Images
    .OrderBy(x => x.DisplayOrder)
    .Select(x => x.ImageUrl)
    .ToArray()
```

### ListedDate

Use the most relevant current listing, preferably the latest published active listing.

### Agent

If no agent exists, return an empty `AgentDto` rather than null to match your DTO shape.

## Recommended File Additions

### `Nexus.Application`

- `Dtos/AgentDto.cs`
- `Dtos/PropertyDto.cs`
- `Dtos/PropertyListResponse.cs`
- `Dtos/PropertyQueryRequest.cs`
- `Interfaces/IPropertyService.cs`
- `Services/PropertyService.cs`

### `Nexus.Infrastructure`

- `Interfaces/IPropertyRepository.cs`
- `Repositories/PropertyRepository.cs`

### `Nexus.Api`

- update `Controllers/PropertyController.cs`
- update DI registration in `Extensions/AppExtensions.cs`

## Current Model Constraints To Keep In Mind

These are worth keeping explicit before implementation:

- `Property.Id` is `Guid`
- `PropertyType` exists both as:
  - enum in `Nexus.Domain.Enums`
  - entity in `Nexus.Domain.Entities`
- the generic `RepositoryBase<T>` currently assumes `Find(int id)`, so property detail queries should not rely on it
- there is no obvious dedicated features table in the current schema
- there is no obvious dedicated featured-property flag in the current schema

## Recommended Build Order

1. Add DTOs in `Nexus.Application/Dtos`
2. Add `IPropertyService`
3. Add `IPropertyRepository`
4. Implement `PropertyRepository`
5. Implement `PropertyService`
6. Register DI services
7. Implement `PropertyController` endpoints
8. Add tests for service and controller behavior

## What This Means For The Next Step

You can implement the property endpoints cleanly in the current architecture without restructuring the solution.

The only decisions that should be treated as fixed before coding are:

- use the default `PropertyController` route `/api/property`
- use `Guid` for property IDs end-to-end unless you intentionally introduce a separate public integer ID model
