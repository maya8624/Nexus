using Nexus.Application.Common;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;

namespace Nexus.Application.Interfaces.Business
{
    public interface IInvoiceService
    {
        Task<Result<InvoiceResponse>> GetByFileUploadIdAsync(Guid fileUploadId, Guid userId, CancellationToken ct);
        Task<Result<InvoiceResponse>> UpdateAsync(Guid id, UpdateInvoiceRequest request, Guid userId, CancellationToken ct);
    }
}
