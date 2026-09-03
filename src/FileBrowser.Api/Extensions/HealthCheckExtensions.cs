using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FileBrowser.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddDefaultHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return services;
    }
}
