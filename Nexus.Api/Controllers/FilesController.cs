using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces;

namespace Nexus.Api.Controllers
{
    public class FilesController : AppControllerBase
    {
        private readonly IBlobStorageService _blobStorage;

        public FilesController(IBlobStorageService blobStorage)
        {
            _blobStorage = blobStorage;
        }

        [HttpPost("upload-url")]
        [ProducesResponseType(typeof(SasUploadResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<SasUploadResponse>> GetUploadUrl([FromBody] GetUploadUrlRequest request, CancellationToken ct)
        {
            var result = await _blobStorage.GenerateSasUploadUrlAsync(request.FileName, request.ContentType, UserId, ct);

            return result.IsSuccess ? Ok(result.Value) : MapFailure(result);
        }
    }
}
