using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.ReadModels;
using ListingTypeEnum = Nexus.Domain.Enums.ListingType;
using PropertyTypeEnum = Nexus.Domain.Enums.PropertyType;

namespace Nexus.Application.Services
{
    public class PropertyService : IPropertyService
    {
        private readonly IPropertyRepository _propertyRepository;

        public PropertyService(IPropertyRepository propertyRepository)
        {
            _propertyRepository = propertyRepository;
        }

        public async Task<Result<PropertyListResponse>> GetPropertiesAsync(PropertyQueryRequest request, CancellationToken ct)
        {
            int? propertyTypeId = null;
            if (!string.IsNullOrWhiteSpace(request.PropertyType))
                propertyTypeId = (int)Enum.Parse<PropertyTypeEnum>(request.PropertyType, ignoreCase: true);

            ListingTypeEnum? listingType = null;
            if (!string.IsNullOrWhiteSpace(request.ListingType))
                listingType = Enum.Parse<ListingTypeEnum>(request.ListingType, ignoreCase: true);

            var (items, totalCount) = await _propertyRepository.GetPagedAsync(
                request.Skip,
                request.PageSize,
                propertyTypeId,
                listingType,
                ct);

            return Result<PropertyListResponse>.Success(new PropertyListResponse
            {
                Items = items.Select(MapToDto).ToArray(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize)
            });
        }

        public async Task<Result<PropertyDto>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var property = await _propertyRepository.GetByIdAsync(id, ct);
            if (property is null)
                return Result<PropertyDto>.NotFound("PropertyNotFound", "Property not found.");

            return Result<PropertyDto>.Success(MapToDto(property));
        }

        private static PropertyDto MapToDto(PropertyReadModel property)
        {
            var agentName = string.Join(" ", new[] { property.AgentFirstName, property.AgentLastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            var addressLine = string.Join(", ", new[] { property.AddressLine1, property.AddressLine2 }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            return new PropertyDto
            {
                Id = property.Id,
                Title = property.Title,
                Address = addressLine,
                Suburb = property.Suburb,
                State = property.State,
                Postcode = property.Postcode,
                PriceValue = property.PriceValue,
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
    }
}
