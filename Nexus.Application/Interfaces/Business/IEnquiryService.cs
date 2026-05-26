using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;

namespace Nexus.Application.Interfaces.Business
{
    public interface IEnquiryService
    {
        Task<Result<EnquiryResponse>> CreateAsync(CreateEnquiryRequest request, Guid userId, CancellationToken ct);
        Task<Result<IReadOnlyList<EnquiryResponse>>> GetMyEnquiriesAsync(Guid userId, CancellationToken ct);
        Task<Result<EnquiryResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct);
        Task<Result<EnquiryResponse>> UpdateAsync(Guid id, UpdateEnquiryRequest request, Guid userId, CancellationToken ct);
    }
}
