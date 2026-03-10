using CallSense.Analysis.Domain.Interfaces;
using CallSense.Analysis.Infrastructure.Groq;
using CallSense.Analysis.Infrastructure.Messaging;
using CallSense.Analysis.Infrastructure.Persistence;
using CallSense.Analysis.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CallSense.Analysis.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AnalysisDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped<ICallAnalysisRepository, CallAnalysisRepository>();

        services.AddHttpClient<ILlmAnalysisService, GroqLlmAnalysisService>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<CallTranscribedConsumer>();
            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(new Uri(configuration.GetConnectionString("RabbitMQ")!));
                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
