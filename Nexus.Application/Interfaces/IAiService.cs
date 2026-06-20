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
        Task<CopilotResponse> SendMessage(string message, string threadId, CancellationToken ct);
        Task<Result<CopilotResponse>> GetReply(CopilotRequest request, CancellationToken ct);
        Task<Result<PreferenceSearchResponse>> GetPreferenceProperties(TenantPreferenceRequest request, Guid userId, CancellationToken ct);
        Task<Result<SuburbSummaryResponse>> GetSuburbSummary(SuburbSummaryRequest request, Guid userId, CancellationToken ct);
        Task<Result<EnquiryDraftResponse>> GetEnquiryDraft(EnquiryDraftRequest request, CancellationToken ct);
        IAsyncEnumerable<string> StreamReply(CopilotRequest request, CancellationToken ct);
        Task<Result<DocumentIngestionResponse>> IngestDocumentAsync(byte[] fileBytes, string fileName, string? propertyId, string? docType, CancellationToken ct);
        Task<Result<InvoiceExtractionResponse>> ExtractInvoiceAsync(byte[] fileBytes, string fileName, CancellationToken ct);
    }
}
