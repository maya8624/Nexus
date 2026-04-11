using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Interfaces
{
    public interface IAiService
    {
        Task<ChatResponse> SendMessage(string message, string sessionId, CancellationToken ct);
        Task<Result<ChatResponse>> GetAnswer(string message, string sessionId, CancellationToken ct);
    }
}
