using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos.Responses
{
    public class ChatResponse
    {
        public required string Answer { get; init; }
        public required string ThreadId { get; init; }
    }
}
