using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos.Requests
{
    public class CopilotRequest
    {
        public required string Message { get; set; }
        public string? ThreadId { get; set; }
        public string? PropertyId { get; set; }
        public CopilotMetadata? Metadata { get; set; }
    }

    public class CopilotMetadata
    {
        public List<string>? Suburbs { get; set; }       // suburb summary, school catchments
        public string? Intent { get; set; }              // "suburb_summary", "market_trends"
        public int? BudgetMax { get; set; }              // find matching properties
        public bool? PetFriendly { get; set; }           // find matching properties
        public int? BedroomsMin { get; set; }            // find matching properties
        public int? BedroomsMax { get; set; }            // find matching properties
        public int? AvailableWithinDays { get; set; }    // find matching properties
    }

    public class CopilotRequestValidator : AbstractValidator<CopilotRequest>
    {
        public CopilotRequestValidator()
        {
            RuleFor(x => x.Message).NotEmpty().MaximumLength(1000);
        }
    }
}
