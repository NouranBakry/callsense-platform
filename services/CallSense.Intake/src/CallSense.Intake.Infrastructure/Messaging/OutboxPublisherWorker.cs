using System.Text.Json;
using CallSense.Contracts.Messages;
using CallSense.Infrastructure.Shared.Outbox;
using CallSense.Intake.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CallSense.Intake.Infrastructure.Messaging;

public class OutboxPublisherWorker : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherWorker> _logger;

    public OutboxPublisherWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisherWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessBatchAsync(stoppingToken); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { _logger.LogError(ex, "OutboxPublisherWorker error"); }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IntakeDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        foreach (var message in messages)
            await PublishAsync(bus, message, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task PublishAsync(IBus bus, OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            switch (message.Type)
            {
                case nameof(CallTranscribed):
                    var payload = JsonSerializer.Deserialize<CallTranscribed>(message.Payload)!;
                    await bus.Publish(payload, cancellationToken);
                    break;
                default:
                    _logger.LogWarning("Unknown outbox message type: {Type}", message.Type);
                    break;
            }
            message.ProcessedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            message.Error = ex.Message;
            _logger.LogError(ex, "Failed to publish outbox message {Id}", message.Id);
        }
    }
}
