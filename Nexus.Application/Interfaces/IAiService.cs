using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexus.Application.Interfaces
{
    public interface IAiService
    {
        Task<ChatResponse> SendMessage(string message, string threadId, CancellationToken ct);
        Task<Result<ChatResponse>> GetReply(CopilotRequest request, CancellationToken ct);
        Task<Result<PreferenceSearchResponse>> GetPreferenceProperties(TenantPreferenceRequest request, Guid userId, CancellationToken ct);
        Task<Result<SuburbSummaryResponse>> GetSuburbSummary(SuburbSummaryRequest request, Guid userId, CancellationToken ct);
        IAsyncEnumerable<string> StreamReply(CopilotRequest request, CancellationToken ct);
    }
}
