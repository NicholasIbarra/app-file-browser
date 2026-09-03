using FileBrowser.Application.Abtractstions.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileBrowser.Infrastructure.FileSystem;

internal interface IFileSystemFactory
{
    IFileSystem Create();
}

internal sealed class FileSystemFactory : IFileSystemFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<FileBrowserOptions> _options;

    public FileSystemFactory(
        IServiceProvider serviceProvider,
        IOptions<FileBrowserOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options;
    }

    public IFileSystem Create()
    {
        return _options.Value.Provider switch
        {
            FileSystemProvider.Local =>
                _serviceProvider.GetRequiredService<LocalFileSystem>(),

            // TODO: register and return an Azure Blob Storage backed
            // IFileSystem implementation here once one exists.
            FileSystemProvider.Azure => throw new NotSupportedException(
                "The Azure file system provider is not implemented yet. " +
                "Set \"FileBrowser:Provider\" to \"Local\" in configuration."),

            _ => throw new NotSupportedException(
                $"Unknown file system provider '{_options.Value.Provider}'.")
        };
    }
}
