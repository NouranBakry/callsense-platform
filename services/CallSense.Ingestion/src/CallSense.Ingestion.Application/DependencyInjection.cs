using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CallSense.Ingestion.Application;

// Extension method pattern — keeps Program.cs clean.
// The API project calls builder.Services.AddApplicationServices()
// and knows nothing about MediatR or FluentValidation internals.
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // MediatR scans this assembly for all IRequestHandler<,> implementations
        // and registers them automatically. When you add a new command + handler,
        // you don't touch this file — MediatR finds it by convention.
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // FluentValidation scans this assembly for all AbstractValidator<T> classes
        // and registers them as IValidator<T> in DI.
        // The validator pipeline behaviour (not added here) will resolve them by type
        // before each handler call.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
