using FileBrowser.Application.Browsing;
using Microsoft.Extensions.DependencyInjection;

namespace FileBrowser.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IDirectoryBrowserService, DirectoryBrowserService>();

        return services;
    }
}
