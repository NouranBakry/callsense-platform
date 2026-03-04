using Azure.Storage.Blobs;
using CallSense.Ingestion.Domain.Interfaces;
using CallSense.Ingestion.Infrastructure.Messaging;
using CallSense.Ingestion.Infrastructure.Persistence;
using CallSense.Ingestion.Infrastructure.Persistence.Repositories;
using CallSense.Ingestion.Infrastructure.Storage;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CallSense.Ingestion.Infrastructure;

// Called from Program.cs: builder.Services.AddInfrastructureServices(builder.Configuration)
// This method is the Infrastructure layer's single public surface.
// The API project references Infrastructure but doesn't know what's inside —
// it just calls this one method and gets everything wired up.
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── PostgreSQL / EF Core ─────────────────────────────────────────────────
        // UseNpgsql tells EF to use the Npgsql driver (PostgreSQL).
        // The connection string is read from appsettings.json → ConnectionStrings:Postgres.
        // DbContext is Scoped by default — one instance per HTTP request.
        // This is correct: a Scoped DbContext means each request gets its own
        // change-tracker, preventing one request's uncommitted changes from leaking
        // into another request.
        services.AddDbContext<IngestionDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        // ── Repositories ─────────────────────────────────────────────────────────
        // Register the concrete repository as Scoped (matches DbContext lifetime).
        // The interface (ICallRecordRepository) is what the Application layer sees —
        // it never knows EF or Postgres are involved.
        services.AddScoped<ICallRecordRepository, CallRecordRepository>();

        // ── Azure Blob Storage ───────────────────────────────────────────────────
        // BlobServiceClient is thread-safe and manages connection pools internally —
        // Singleton is correct (one instance for the process lifetime).
        // In dev, the connection string points to Azurite (local emulator).
        // In prod, it points to real Azure Storage — same code, different config.
        services.AddSingleton(_ =>
            new BlobServiceClient(configuration.GetConnectionString("AzureBlobStorage")));

        // AzureBlobStorageService is Scoped (fine — it depends on the Singleton
        // BlobServiceClient, so no lifetime mismatch).
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

        // ── MassTransit / RabbitMQ ───────────────────────────────────────────────
        // MassTransit is the messaging abstraction over RabbitMQ.
        // The OutboxPublisherWorker uses IBus (registered by MassTransit here)
        // to publish messages. The Ingestion service only publishes — it has no consumers.
        // Consumers are added in later services (Transcription, Analysis).
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((ctx, cfg) =>
            {
                // The connection string is "amqp://user:pass@host:port/vhost"
                // e.g. "amqp://guest:guest@localhost:5672/" for local dev.
                cfg.Host(new Uri(configuration.GetConnectionString("RabbitMQ")!));

                // ConfigureEndpoints scans for consumer classes and wires up their queues.
                // Ingestion has no consumers, but this is required for MassTransit to
                // complete its setup. Leave it in — it's a no-op when there are no consumers.
                cfg.ConfigureEndpoints(ctx);
            });
        });

        // ── Outbox Background Worker ─────────────────────────────────────────────
        // AddHostedService registers OutboxPublisherWorker as a BackgroundService.
        // ASP.NET Core starts it automatically when the app starts and stops it
        // when the app shuts down. It runs in the same process as the API,
        // invisible to Kubernetes (which only sees HTTP health checks).
        services.AddHostedService<OutboxPublisherWorker>();

        return services;
    }
}
