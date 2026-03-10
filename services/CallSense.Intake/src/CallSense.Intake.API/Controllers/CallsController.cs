using CallSense.Intake.Application;
using CallSense.Intake.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CallSense.Intake.API.Controllers;

[ApiController]
[Route("api/calls")]
public class CallsController : ControllerBase
{
    private readonly ICallService _callService;

    public CallsController(ICallService callService) => _callService = callService;

    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(tenantIdHeader, out var tenantId))
            return BadRequest(new { error = "X-Tenant-Id header is required and must be a valid Guid." });

        // Copy to MemoryStream — IFormFile.OpenReadStream() is non-seekable and
        // gets disposed by the framework before we can seek back for blob upload
        var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var request = new UploadCallRequest(
            TenantId: tenantId,
            FileName: file.FileName,
            FileContent: memoryStream,
            ContentType: file.ContentType,
            FileSizeBytes: file.Length);

        var result = await _callService.UploadAsync(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, new { callId = result.Value });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(tenantIdHeader, out var tenantId))
            return BadRequest(new { error = "X-Tenant-Id header is required and must be a valid Guid." });

        var record = await _callService.GetByIdAsync(id, tenantId, cancellationToken);
        if (record is null) return NotFound();

        return Ok(new
        {
            record.Id,
            record.TenantId,
            record.OriginalFileName,
            record.BlobUrl,
            record.FileSizeBytes,
            Status = record.Status.ToString(),
            record.TranscriptText,
            record.CreatedAt
        });
    }
}
