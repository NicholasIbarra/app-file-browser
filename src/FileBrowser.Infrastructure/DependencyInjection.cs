using Azure.Identity;
using Azure.Storage.Blobs;
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

        switch (options.Provider)
        {
            case FileSystemProvider.Local:
                // A freshly cloned repo has no local storage folder yet - it's
                // gitignored - so create it on startup instead of failing the
                // first request.
                if (options.CreateIfMissing)
                {
                    Directory.CreateDirectory(options.HomeDirectory);
                }

                services.AddSingleton<IFileSystemPathResolver, FileSystemPathResolver>();
                services.AddSingleton<IFileSystem, LocalFileSystem>();
                break;

            case FileSystemProvider.Azure:
                services.AddSingleton<IFileSystem>(_ => CreateAzureBlobFileSystem(configuration));
                break;

            default:
                throw new NotSupportedException(
                    $"Unknown file system provider '{options.Provider}'.");
        }

        return services;
    }

    private static AzureBlobFileSystem CreateAzureBlobFileSystem(IConfiguration configuration)
    {
        var azureOptions = new AzureBlobOptions();
        configuration.GetSection(AzureBlobOptions.SectionName).Bind(azureOptions);

        if (string.IsNullOrWhiteSpace(azureOptions.ContainerName))
        {
            throw new InvalidOperationException(
                $"\"{AzureBlobOptions.SectionName}:ContainerName\" must be configured " +
                "when \"FileBrowser:Provider\" is \"Azure\".");
        }

        BlobServiceClient serviceClient;

        if (!string.IsNullOrWhiteSpace(azureOptions.ConnectionString))
        {
            serviceClient = new BlobServiceClient(azureOptions.ConnectionString);
        }
        else if (!string.IsNullOrWhiteSpace(azureOptions.AccountUrl))
        {
            serviceClient = new BlobServiceClient(
                new Uri(azureOptions.AccountUrl),
                new DefaultAzureCredential());
        }
        else
        {
            throw new InvalidOperationException(
                $"Either \"{AzureBlobOptions.SectionName}:ConnectionString\" or " +
                $"\"{AzureBlobOptions.SectionName}:AccountUrl\" must be configured " +
                "when \"FileBrowser:Provider\" is \"Azure\".");
        }

        var containerClient = serviceClient.GetBlobContainerClient(azureOptions.ContainerName);
        containerClient.CreateIfNotExists();

        return new AzureBlobFileSystem(containerClient);
    }
}
