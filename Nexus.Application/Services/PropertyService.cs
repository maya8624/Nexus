using FluentValidation;
using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Interfaces;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Interfaces;
using Nexus.Infrastructure.Responses;
using PropertyTypeEnum = Nexus.Domain.Enums.PropertyType;

namespace Nexus.Application.Services
{
    public class PropertyService : IPropertyService
    {
        private readonly IPropertyRepository _propertyRepository;
        private readonly IValidator<PropertyQueryRequest> _queryValidator;
        private readonly IValidator<CreateInspectionBookingRequest> _createBookingValidator;
        private readonly IValidator<CheckInspectionAvailabilityRequest> _availabilityValidator;
        private readonly IUnitOfWork _uow;

        public PropertyService(
            IPropertyRepository propertyRepository,
            IValidator<PropertyQueryRequest> queryValidator,
            IValidator<CreateInspectionBookingRequest> createBookingValidator,
            IValidator<CheckInspectionAvailabilityRequest> availabilityValidator,
            IUnitOfWork uow)
        {
            _propertyRepository = propertyRepository;
            _queryValidator = queryValidator;
            _createBookingValidator = createBookingValidator;
            _availabilityValidator = availabilityValidator;
            _uow = uow;
        }

        public async Task<PropertyListResponse> GetProperties(int page, int pageSize, string? type, CancellationToken ct)
        {
            var query = NormalizeQuery(page, pageSize, type);
            await _queryValidator.ValidateAndThrowAsync(query, ct);

            PropertyTypeEnum? parsedType = null;
            if (string.IsNullOrWhiteSpace(query.Type) == false)
            {
                parsedType = Enum.Parse<PropertyTypeEnum>(query.Type, ignoreCase: true);
            }

            var (items, totalCount) = await _propertyRepository.GetPagedProperties(
                query.Page,
                query.PageSize,
                parsedType,
                ct);

            return new PropertyListResponse
            {
                Items = items.Select(MapToDto).ToArray(),
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
                TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
            };
        }

        public async Task<PropertyDto?> GetPropertyById(Guid id, CancellationToken ct)
        {
            var property = await _propertyRepository.GetPropertyById(id, ct);
            return property == null ? null : MapToDto(property);
        }

        public async Task<Result<InspectionBookingDto>> CreateInspectionBookingAsync(CreateInspectionBookingRequest request, CancellationToken ct)
        {
            var validationResult = await _createBookingValidator.ValidateAsync(request, ct);
            if (validationResult.IsValid == false)
            {
                return Result<InspectionBookingDto>.ValidationError(MapValidationErrors(validationResult));
            }

            var userExists = await _propertyRepository.UserExists(request.UserId, ct);
            if (userExists == false)
            {
                return Result<InspectionBookingDto>.NotFound("UserNotFound", "The specified user was not found.");
            }

            var bookingContext = await _propertyRepository.GetBookingContext(request.PropertyId, request.ListingId, ct);
            if (bookingContext == null || bookingContext.PropertyIsActive == false)
            {
                return Result<InspectionBookingDto>.NotFound("PropertyNotFound", "The specified property was not found or is inactive.");
            }

            if (bookingContext.ListingIsPublished == false || IsListingActive(bookingContext.ListingStatus) == false)
            {
                return Result<InspectionBookingDto>.Conflict("ListingUnavailable", "The specified listing is not available for inspection booking.");
            }

            if (request.AgentId.HasValue)
            {
                var agentExists = await _propertyRepository.AgentExists(request.AgentId.Value, ct);
                if (agentExists == false)
                {
                    return Result<InspectionBookingDto>.NotFound("AgentNotFound", "The specified agent was not found.");
                }

                if (bookingContext.ListingAgentId.HasValue && bookingContext.ListingAgentId.Value != request.AgentId.Value)
                {
                    return Result<InspectionBookingDto>.Conflict("AgentMismatch", "The specified agent does not match the listing agent.");
                }
            }

            var hasDuplicateBooking = await _propertyRepository.HasDuplicateBooking(
                request.UserId,
                request.PropertyId,
                request.ListingId,
                request.InspectionStartAtUtc,
                request.InspectionEndAtUtc,
                ct);

            if (hasDuplicateBooking)
            {
                return Result<InspectionBookingDto>.Conflict("DuplicateBooking", "A booking request already exists for the same user, property, listing, and time window.");
            }

            var hasConfirmedConflict = await _propertyRepository.HasOverlappingConfirmedBooking(
                request.PropertyId,
                request.InspectionStartAtUtc,
                request.InspectionEndAtUtc,
                excludeBookingId: null,
                ct);

            if (hasConfirmedConflict)
            {
                return Result<InspectionBookingDto>.Conflict("BookingConflict", "The requested inspection time overlaps an existing confirmed booking.");
            }

            var now = DateTimeOffset.UtcNow;
            var booking = new InspectionBooking
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                PropertyId = request.PropertyId,
                ListingId = request.ListingId,
                AgentId = request.AgentId ?? bookingContext.ListingAgentId,
                InspectionStartAtUtc = request.InspectionStartAtUtc,
                InspectionEndAtUtc = request.InspectionEndAtUtc,
                Status = InspectionBookingStatus.Pending,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await _propertyRepository.AddInspectionBooking(booking, ct);
            await _uow.SaveChanges();

            return Result<InspectionBookingDto>.Success(MapInspectionBookingDto(booking));
        }

        public async Task<Result<InspectionBookingDto>> CancelInspectionBookingAsync(Guid id, CancellationToken ct)
        {
            var booking = await _propertyRepository.GetInspectionBookingForUpdate(id, ct);
            if (booking == null)
            {
                return Result<InspectionBookingDto>.NotFound("BookingNotFound", "The specified booking was not found.");
            }

            if (booking.Status != InspectionBookingStatus.Pending && booking.Status != InspectionBookingStatus.Confirmed)
            {
                return Result<InspectionBookingDto>.Conflict("InvalidBookingStatus", "Only pending or confirmed bookings can be cancelled.");
            }

            booking.Status = InspectionBookingStatus.Cancelled;
            booking.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await _uow.SaveChanges();

            return Result<InspectionBookingDto>.Success(MapInspectionBookingDto(booking));
        }

        public async Task<Result<InspectionBookingDto>> GetInspectionBookingByIdAsync(Guid id, CancellationToken ct)
        {
            var booking = await _propertyRepository.GetInspectionBookingById(id, ct);
            if (booking == null)
            {
                return Result<InspectionBookingDto>.NotFound("BookingNotFound", "The specified booking was not found.");
            }

            return Result<InspectionBookingDto>.Success(MapInspectionBookingDto(booking));
        }

        public async Task<Result<InspectionAvailabilityResponse>> CheckInspectionAvailabilityAsync(CheckInspectionAvailabilityRequest request, CancellationToken ct)
        {
            var validationResult = await _availabilityValidator.ValidateAsync(request, ct);
            if (validationResult.IsValid == false)
            {
                return Result<InspectionAvailabilityResponse>.ValidationError(MapValidationErrors(validationResult));
            }

            var bookingContext = await _propertyRepository.GetBookingContext(request.PropertyId, request.ListingId, ct);
            if (bookingContext == null || bookingContext.PropertyIsActive == false)
            {
                return Result<InspectionAvailabilityResponse>.NotFound("PropertyNotFound", "The specified property was not found or is inactive.");
            }

            if (bookingContext.ListingIsPublished == false || IsListingActive(bookingContext.ListingStatus) == false)
            {
                return Result<InspectionAvailabilityResponse>.Conflict("ListingUnavailable", "The specified listing is not available for inspection booking.");
            }

            var hasConfirmedConflict = await _propertyRepository.HasOverlappingConfirmedBooking(
                request.PropertyId,
                request.InspectionStartAtUtc,
                request.InspectionEndAtUtc,
                request.ExcludeBookingId,
                ct);

            return Result<InspectionAvailabilityResponse>.Success(new InspectionAvailabilityResponse
            {
                IsAvailable = hasConfirmedConflict == false,
                Message = hasConfirmedConflict
                    ? "The requested inspection time conflicts with an existing confirmed booking."
                    : "The requested inspection time is available."
            });
        }

        private static PropertyQueryRequest NormalizeQuery(int page, int pageSize, string? type)
        {
            return new PropertyQueryRequest
            {
                Page = page < 1 ? 1 : page,
                PageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100),
                Type = string.IsNullOrWhiteSpace(type) ? null : type.Trim()
            };
        }

        private static PropertyDto MapToDto(PropertyReadModel property)
        {
            var agentName = string.Join(" ", new[] { property.AgentFirstName, property.AgentLastName }
                    .Where(x => string.IsNullOrWhiteSpace(x) == false));

            var addressLine = string.Join(", ", new[]
            {
                property.AddressLine1,
                property.AddressLine2
            }.Where(x => string.IsNullOrWhiteSpace(x) == false));

            return new PropertyDto
            {
                Id = property.Id,
                Title = property.Title,
                Address = addressLine,
                Suburb = property.Suburb,
                State = property.State,
                Postcode = property.Postcode,
                PriceValue = property.PriceValue,
                Price = FormatPrice(property.PriceValue),
                PropertyType = property.PropertyType,
                Bedrooms = property.Bedrooms,
                Bathrooms = property.Bathrooms,
                Parking = property.Parking,
                LandSize = property.LandSizeSqm.HasValue ? (int)Math.Round(property.LandSizeSqm.Value) : 0,
                Description = property.Description,
                Features = Array.Empty<string>(),
                Images = property.Images.ToArray(),
                Agent = new AgentDto
                {
                    Name = agentName,
                    Phone = property.AgentPhone,
                    Agency = property.AgencyName,
                    Photo = property.AgentPhoto
                },
                AuctionDate = null,
                IsNew = property.ListedAtUtc != null && property.ListedAtUtc >= DateTimeOffset.UtcNow.AddDays(-14),
                IsFeatured = false,
                InspectionTimes = Array.Empty<string>(),
                ListedDate = property.ListedAtUtc?.ToString("yyyy-MM-dd") ?? string.Empty
            };
        }

        private static InspectionBookingDto MapInspectionBookingDto(InspectionBooking booking)
        {
            return new InspectionBookingDto
            {
                Id = booking.Id,
                UserId = booking.UserId,
                PropertyId = booking.PropertyId,
                ListingId = booking.ListingId ?? Guid.Empty,
                AgentId = booking.AgentId,
                InspectionStartAtUtc = booking.InspectionStartAtUtc,
                InspectionEndAtUtc = booking.InspectionEndAtUtc ?? booking.InspectionStartAtUtc,
                Status = booking.Status.ToString(),
                Notes = booking.Notes,
                CreatedAtUtc = booking.CreatedAtUtc,
                UpdatedAtUtc = booking.UpdatedAtUtc
            };
        }

        private static IReadOnlyList<ResultError> MapValidationErrors(FluentValidation.Results.ValidationResult validationResult)
        {
            return validationResult.Errors
                .Select(x => new ResultError(x.PropertyName, x.ErrorMessage))
                .ToArray();
        }

        private static string FormatPrice(decimal price)
        {
            return price > 0 ? price.ToString("C0") : string.Empty;
        }

        private static bool IsListingActive(string listingStatus)
        {
            return Enum.TryParse<ListingStatus>(listingStatus, ignoreCase: true, out var parsedStatus)
                && parsedStatus == ListingStatus.Active;
        }
    }
}
