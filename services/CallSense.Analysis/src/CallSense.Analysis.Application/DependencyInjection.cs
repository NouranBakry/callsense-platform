using CallSense.Analysis.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CallSense.Analysis.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICallAnalysisAppService, CallAnalysisAppService>();
        return services;
    }
}
