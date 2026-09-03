using FileBrowser.Application.Abtractstions.FileSystem;
using FileBrowser.Infrastructure.FileSystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileBrowser.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(contentRootPath);

        var options = new FileBrowserOptions();
        configuration.GetSection(FileBrowserOptions.SectionName).Bind(options);

        if (!Path.IsPathRooted(options.HomeDirectory))
        {
            options.HomeDirectory = Path.GetFullPath(
                Path.Combine(contentRootPath, options.HomeDirectory));
        }

        // A freshly cloned repo has no local storage folder yet - it's
        // gitignored - so create it on startup for the Local provider
        // instead of failing the first request.
        if (options.Provider == FileSystemProvider.Local && options.CreateIfMissing)
        {
            Directory.CreateDirectory(options.HomeDirectory);
        }

        services.AddSingleton(Options.Create(options));

        services.AddSingleton<IFileSystemPathResolver, FileSystemPathResolver>();
        services.AddSingleton<LocalFileSystem>();
        services.AddSingleton<IFileSystemFactory, FileSystemFactory>();
        services.AddSingleton<IFileSystem>(
            sp => sp.GetRequiredService<IFileSystemFactory>().Create());

        return services;
    }
}
