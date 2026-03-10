using System.Text.Json;
using CallSense.Analysis.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CallSense.Analysis.API.Controllers;

[ApiController]
[Route("api/analyses")]
public class AnalysisController : ControllerBase
{
    private readonly ICallAnalysisAppService _analysisService;

    public AnalysisController(ICallAnalysisAppService analysisService) => _analysisService = analysisService;

    [HttpGet("{callId:guid}")]
    public async Task<IActionResult> GetByCallId(
        Guid callId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(tenantIdHeader, out var tenantId))
            return BadRequest(new { error = "X-Tenant-Id header is required and must be a valid Guid." });

        var analysis = await _analysisService.GetByCallIdAsync(callId, tenantId, cancellationToken);
        if (analysis is null) return NotFound();

        return Ok(new
        {
            analysis.Id,
            analysis.CallId,
            analysis.TenantId,
            analysis.OverallScore,
            analysis.Report,
            Strengths = analysis.Strengths != null ? JsonSerializer.Deserialize<string[]>(analysis.Strengths) : null,
            Improvements = analysis.Improvements != null ? JsonSerializer.Deserialize<string[]>(analysis.Improvements) : null,
            Status = analysis.Status.ToString(),
            analysis.ErrorMessage,
            analysis.CreatedAt
        });
    }
}
