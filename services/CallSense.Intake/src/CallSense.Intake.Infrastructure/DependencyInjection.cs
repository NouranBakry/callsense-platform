using Azure.Storage.Blobs;
using CallSense.Intake.Domain.Interfaces;
using CallSense.Intake.Infrastructure.Messaging;
using CallSense.Intake.Infrastructure.Persistence;
using CallSense.Intake.Infrastructure.Persistence.Repositories;
using CallSense.Intake.Infrastructure.Storage;
using CallSense.Intake.Infrastructure.Transcription;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CallSense.Intake.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<IntakeDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped<ICallRecordRepository, CallRecordRepository>();

        services.AddSingleton(_ => new BlobServiceClient(configuration.GetConnectionString("AzureBlobStorage")));
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

        services.AddHttpClient<ITranscriptionService, GroqTranscriptionService>();

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(new Uri(configuration.GetConnectionString("RabbitMQ")!));
                cfg.ConfigureEndpoints(ctx);
            });
        });

        services.AddHostedService<OutboxPublisherWorker>();

        return services;
    }
}
