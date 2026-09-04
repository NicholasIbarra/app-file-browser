using FileBrowser.Application.Abtractstions.FileSystem;
using FileBrowser.Application.Abtractstions.Indexing;
using FileBrowser.Application.Browsing;
using FileBrowser.Application.Search;
using FileBrowser.Infrastructure.BackgroundServices;
using FileBrowser.Infrastructure.FileSystem;
using FileBrowser.Infrastructure.Indexing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddScoped<IDirectoryBrowserService, DirectoryBrowserService>();
        services.AddScoped<IFileSearchService, FileSearchService>();
        services.AddSingleton<IFileSearchIndex, FileSearchIndex>();
        services.AddSingleton<IFileSearchScorer, FileSearchScorer>();
        services.AddHostedService<FileIndexHostedService>();

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

        services.AddSingleton<IFileSystemPathResolver, FileSystemPathResolver>();

        switch (options.Provider)
        {
            case FileSystemProvider.Local:
                services.AddSingleton<IFileSystem, LocalFileSystem>();
                break;

            case FileSystemProvider.Azure:
                throw new NotSupportedException(
                    "The Azure file system provider is not implemented yet. " +
                    "Set \"FileBrowser:Provider\" to \"Local\" in configuration.");

            default:
                throw new NotSupportedException(
                    $"Unknown file system provider '{options.Provider}'.");
        }

        return services;
    }
}
