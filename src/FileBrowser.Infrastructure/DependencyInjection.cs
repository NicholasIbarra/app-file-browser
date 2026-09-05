using Azure.Storage.Blobs;
using FileBrowser.Application;
using FileBrowser.Application.Abtractstions.BackgroundJobs;
using FileBrowser.Application.Abtractstions.FileSystem;
using FileBrowser.Application.Abtractstions.Indexing;
using FileBrowser.Application.Browsing;
using FileBrowser.Application.Search;
using FileBrowser.Infrastructure.BackgroundJobs;
using FileBrowser.Infrastructure.BackgroundServices;
using FileBrowser.Infrastructure.FileSystem;
using FileBrowser.Infrastructure.Indexing;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace FileBrowser.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath, 
        Assembly apiAssembly
        )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(contentRootPath);

        services.AddScoped<IDirectoryBrowserService, DirectoryBrowserService>();
        services.AddScoped<FileUploadJob>();
        services.AddScoped<IFileSearchService, FileSearchService>();
        
        services.AddSingleton<IFileSearchIndex, FileSearchIndex>();
        services.AddSingleton<IFileSearchScorer, FileSearchScorer>();
        services.AddSingleton<IFileSystemPathResolver, FileSystemPathResolver>();

        services.AddHostedService<FileIndexHostedService>();

        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblies([typeof(FileBrowserApplication).Assembly, apiAssembly]));

        services.AddHangfire(configuration =>
        {
            configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseMemoryStorage();
        });

        services.AddScoped<IBackgroundJobManager, HangfireBackgroundJobManager>();

        var options = new FileBrowserOptions();
        configuration.GetSection(FileBrowserOptions.SectionName).Bind(options);

        if (options.Provider == FileSystemProvider.Local && !Path.IsPathRooted(options.HomeDirectory))
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

        switch (options.Provider)
        {
            case FileSystemProvider.Local:
                services.AddSingleton<IFileSystem, LocalFileSystem>();
                break;

            case FileSystemProvider.Azure:
                ValidateAzureOptions(options.Azure);
                services.AddSingleton(CreateBlobContainerClient(options.Azure));
                services.AddSingleton<IFileSystem, AzureFileSystem>();
                break;

            default:
                throw new NotSupportedException(
                    $"Unknown file system provider '{options.Provider}'.");
        }




        return services;
    }

    private static BlobContainerClient CreateBlobContainerClient(AzureFileSystemOptions options)
    {
        return new BlobContainerClient(options.ConnectionString, options.ContainerName);
    }

    private static void ValidateAzureOptions(AzureFileSystemOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ContainerName))
        {
            throw new InvalidOperationException(
                "FileBrowser:Azure:ContainerName is required for the Azure provider.");
        }

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException(
                "FileBrowser:Azure:ConnectionString is required for the Azure provider.");
        }
    }
}
