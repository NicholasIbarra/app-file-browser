using FileBrowser.Application.Abtractstions.Indexing;
using FileBrowser.Application.Files.Events;
using MediatR;

namespace FileBrowser.Application.Files.EventHandlers;

public sealed class FileMovedEventHandler : INotificationHandler<FileMovedEvent>
{
    private readonly IFileSearchIndex _index;

    public FileMovedEventHandler(IFileSearchIndex index)
    {
        _index = index;
    }

    public Task Handle(FileMovedEvent notification, CancellationToken cancellationToken)
    {
        return _index.MoveAsync(
            notification.SourcePath,
            notification.DestinationPath,
            cancellationToken);
    }
}
