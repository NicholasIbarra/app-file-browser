using FileBrowser.Application.Abtractstions.Indexing;
using FileBrowser.Application.Files.Events;
using MediatR;

namespace FileBrowser.Application.Files.EventHandlers;

public sealed class FileRenamedEventHandler : INotificationHandler<FileRenamedEvent>
{
    private readonly IFileSearchIndex _index;

    public FileRenamedEventHandler(IFileSearchIndex index)
    {
        _index = index;
    }

    public Task Handle(FileRenamedEvent notification, CancellationToken cancellationToken)
    {
        return _index.MoveAsync(
            notification.SourcePath,
            notification.NewName,
            cancellationToken);
    }
}
