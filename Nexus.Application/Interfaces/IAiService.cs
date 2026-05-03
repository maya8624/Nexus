using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using System;

namespace Nexus.Application.Interfaces
{
    public interface IAiService
    {
        Task<ChatResponse> SendMessage(string message, string threadId, CancellationToken ct);
        Task<Result<ChatResponse>> GetReply(ChatRequest request, CancellationToken ct);
        IAsyncEnumerable<string> StreamReply(ChatRequest request, CancellationToken ct);
    }
}
