using FluentValidation;
using Nexus.Application.Dtos;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.ReadModels;
using PropertyTypeEnum = Nexus.Domain.Enums.PropertyType;

namespace Nexus.Application.Services
{
    public class PropertyService : IPropertyService
    {
        private readonly IPropertyRepository _propertyRepository;
        private readonly IUnitOfWork _uow;

        public PropertyService(IPropertyRepository propertyRepository, IUnitOfWork uow)
        {
            _propertyRepository = propertyRepository;
            _uow = uow;
        }

        public async Task<PropertyListResponse> GetProperties(PropertyQueryRequest request, CancellationToken ct)
        {
            int? propertyTypeId = null;
            if (string.IsNullOrWhiteSpace(request.Type) == false)
            {
                propertyTypeId =(int)Enum.Parse<PropertyTypeEnum>(request.Type, ignoreCase: true); 
            }

            var (items, totalCount) = await _propertyRepository.GetPagedProperties(
                request.Skip,
                request.PageSize,
                propertyTypeId,
                ct);

            return new PropertyListResponse
            {
                Items = items.Select(MapToDto).ToArray(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }

        public async Task<PropertyDto?> GetPropertyById(Guid id, CancellationToken ct)
        {
            var property = await _propertyRepository.GetPropertyById(id, ct);
            return property == null ? null : MapToDto(property);
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
