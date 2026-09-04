using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileBrowser.Api.Extensions;

public static partial class ControllersExtensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";
    private const string CoreCorsPolicyName = "CorePolicy";

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Mapped in every environment: Azure Container Apps / App Service
        // health probes hit these in Production, not just locally.
        app.MapHealthChecks(HealthEndpointPath);
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }

    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(CoreCorsPolicyName, policy =>
            {
                policy.AllowAnyMethod().AllowAnyHeader();

                // No origins configured (e.g. local dev): allow any origin.
                // In Azure, set "Cors:AllowedOrigins" to the client app's URL.
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins);
                }
                else
                {
                    policy.AllowAnyOrigin();
                }
            });
        });

        return services;
    }

    public static WebApplication UseDefaultCorsPolicy(this WebApplication app)
    {
        app.UseCors(CoreCorsPolicyName);
        
        return app;
    }

    public static IServiceCollection AddEndpointControllers(this IServiceCollection services) 
    {

        services.AddControllers(options =>
        {
            options.Conventions.Add(new RouteTokenTransformerConvention(new KebabParameterTransformer()));
            options.Filters.Add<EnforceProblemDetailsFilter>();
        }).AddJsonOptions(options =>
        {
            var json = options.JsonSerializerOptions;

            json.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            json.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;

            json.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            json.Converters.Add(new JsonStringEnumConverter());

            json.PropertyNameCaseInsensitive = true;
        });

        services.AddEndpointsApiExplorer();
        
        return services;
    }
}

