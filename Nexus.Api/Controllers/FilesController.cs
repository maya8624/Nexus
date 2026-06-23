using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces.Business;
using Nexus.Domain.Enums;

namespace Nexus.Api.Controllers
{
    public class FilesController : AppControllerBase
    {
        private readonly IFileUploadService _fileUploadService;

        public FilesController(IFileUploadService fileUploadService)
        {
            _fileUploadService = fileUploadService;
        }

        [HttpPost("upload-url")]
        [ProducesResponseType(typeof(FileUploadInitiatedResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<FileUploadInitiatedResponse>> GetUploadUrl([FromBody] GetUploadUrlRequest request, CancellationToken ct)
        {
            var result = await _fileUploadService.InitiateAsync(request.FileName, request.ContentType, request.Purpose, UserId, ct);
            return result.IsSuccess ? Ok(result.Value) : MapFailure(result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<FileUploadResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<FileUploadResponse>>> GetByPurpose([FromQuery] UploadPurpose purpose, [FromQuery] UploadStatus? status, CancellationToken ct)
        {
            var result = await _fileUploadService.GetByPurposeAsync(purpose, status, ct);
            return result.IsSuccess ? Ok(result.Value) : MapFailure(result);
        }

        [HttpPost("{id:guid}/confirm")]
        [ProducesResponseType(typeof(FileUploadInitiatedResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<FileUploadInitiatedResponse>> ConfirmUpload(Guid id, [FromBody] ConfirmUploadRequest request, CancellationToken ct)
        {
            var result = await _fileUploadService.ConfirmAsync(id, UserId, request.FileSizeBytes, ct);
            return result.IsSuccess ? Ok(result.Value) : MapFailure(result);
        }
    }
}
