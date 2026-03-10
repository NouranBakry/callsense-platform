using CallSense.Intake.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CallSense.Intake.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICallService, CallService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
