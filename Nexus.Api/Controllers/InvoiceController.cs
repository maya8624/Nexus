using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces.Business;

namespace Nexus.Api.Controllers
{
    [Route("api/invoices")]
    public class InvoiceController : AppControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet("by-file-upload/{fileUploadId:guid}")]
        [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<InvoiceResponse>> GetByFileUpload(Guid fileUploadId, CancellationToken ct)
        {
            var result = await _invoiceService.GetByFileUploadIdAsync(fileUploadId, UserId, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }

        [HttpPatch("{id:guid}")]
        [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<InvoiceResponse>> Update(Guid id, [FromBody] UpdateInvoiceRequest request, CancellationToken ct)
        {
            var result = await _invoiceService.UpdateAsync(id, request, UserId, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }
    }
}
