using FluentValidation;
using Nexus.Application.Dtos;
using Nexus.Application.Interfaces;
using Nexus.Infrastructure.Interfaces;
using Nexus.Infrastructure.Responses;
using PropertyTypeEnum = Nexus.Domain.Enums.PropertyType;

namespace Nexus.Application.Services
{
    public class PropertyService : IPropertyService
    {
        private readonly IPropertyRepository _propertyRepository;
        private readonly IValidator<PropertyQueryRequest> _queryValidator;

        public PropertyService(
            IPropertyRepository propertyRepository,
            IValidator<PropertyQueryRequest> queryValidator)
        {
            _propertyRepository = propertyRepository;
            _queryValidator = queryValidator;
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

        private static string FormatPrice(decimal price)
        {
            return price > 0 ? price.ToString("C0") : string.Empty;
        }
    }
}
