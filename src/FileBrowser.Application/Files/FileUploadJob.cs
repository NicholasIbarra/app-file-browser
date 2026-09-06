using FileBrowser.Application.Abtractstions.FileSystem;
using FileBrowser.Application.Files.Events;
using MediatR;

namespace FileBrowser.Application.Files;

public sealed class FileUploadJob(IFileSystem fileSystem, IPublisher publisher)
{
    public async Task ExecuteAsync(
        string path,
        byte[] content,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        // artificial delay to simulate a long-running upload process
        await Task.Delay(5000, cancellationToken);

        using var stream = new MemoryStream(content, writable: false);

        await fileSystem.UploadAsync(path, stream, overwrite, cancellationToken);
        await publisher.Publish(new FileCreatedEvent(path), cancellationToken);
    }
}
