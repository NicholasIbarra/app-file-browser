using FileBrowser.Application.Abtractstions.FileSystem;
using FileBrowser.Application.Files.Events;
using MediatR;

namespace FileBrowser.Application.Files.BackgroundJobs;

public sealed class FileRenameJob(IFileSystem fileSystem, IPublisher publisher)
{
    public async Task ExecuteAsync(
        string sourcePath, 
        string newName, 
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        await fileSystem.RenameAsync(sourcePath, newName, overwrite, cancellationToken);

        await publisher.Publish(new FileRenamedEvent(sourcePath, newName), cancellationToken);
    }
}
