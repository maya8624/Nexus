using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos.Responses
{
    public sealed class InspectionAvailabilityResponse
    {
        public bool IsAvailable { get; init; }

        public string Message { get; init; } = string.Empty;
    }
}
