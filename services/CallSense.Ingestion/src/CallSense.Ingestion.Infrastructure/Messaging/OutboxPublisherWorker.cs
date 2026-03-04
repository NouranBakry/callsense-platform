using System.Text.Json;
using CallSense.Contracts.Messages;
using CallSense.Infrastructure.Shared.Outbox;
using CallSense.Ingestion.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CallSense.Ingestion.Infrastructure.Messaging;

// BackgroundService is a base class from Microsoft.Extensions.Hosting.
// It runs in the background alongside your API process — same process, separate thread.
// Kubernetes doesn't know about it; it's invisible to the outside world.
public class OutboxPublisherWorker : BackgroundService
{
    // How often to check for unpublished outbox messages
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    // How many messages to process per cycle — avoids overwhelming RabbitMQ on startup
    private const int BatchSize = 20;

    // IServiceScopeFactory because DbContext is Scoped (per-request lifetime),
    // but this worker is a Singleton. You can't inject Scoped into Singleton directly.
    // Solution: create a new scope each cycle, get a fresh DbContext from it.
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherWorker> _logger;

    public OutboxPublisherWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // ExecuteAsync is called once when the app starts.
    // It loops until the app shuts down (cancellationToken is cancelled).
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisherWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Log but don't crash the worker — it will retry next cycle
                _logger.LogError(ex, "OutboxPublisherWorker encountered an error");
            }

            // Wait before next cycle
            await Task.Delay(PollingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxPublisherWorker stopped");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        // New scope = fresh DbContext and IBus for this cycle
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IngestionDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        // Fetch a batch of unprocessed messages, oldest first
        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        _logger.LogDebug("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            await PublishMessageAsync(bus, message, cancellationToken);
        }

        // Save all ProcessedAt timestamps in one round-trip
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task PublishMessageAsync(IBus bus, OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            // Deserialize the JSON payload back into the correct message type
            // and publish it to RabbitMQ via MassTransit
            switch (message.Type)
            {
                case nameof(CallRecordingUploaded):
                    var payload = JsonSerializer.Deserialize<CallRecordingUploaded>(message.Payload)!;
                    await bus.Publish(payload, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Unknown outbox message type: {Type}", message.Type);
                    break;
            }

            // Mark as processed — only reached if Publish succeeded
            message.ProcessedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            // Don't mark as processed — it will be retried next cycle
            message.Error = ex.Message;
            _logger.LogError(ex, "Failed to publish outbox message {Id} of type {Type}", message.Id, message.Type);
        }
    }
}
