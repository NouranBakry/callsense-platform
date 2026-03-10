using CallSense.Analysis.Application.Services;
using CallSense.Contracts.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CallSense.Analysis.Infrastructure.Messaging;

public class CallTranscribedConsumer : IConsumer<CallTranscribed>
{
    private readonly ICallAnalysisAppService _analysisService;
    private readonly ILogger<CallTranscribedConsumer> _logger;

    public CallTranscribedConsumer(ICallAnalysisAppService analysisService, ILogger<CallTranscribedConsumer> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CallTranscribed> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Received CallTranscribed for call {CallId}", msg.CallId);
        await _analysisService.ProcessAsync(msg.CallId, msg.TenantId, msg.TranscriptText, context.CancellationToken);
    }
}
