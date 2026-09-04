using FileBrowser.Application.Abtractstions.Indexing;
using MediatR;

namespace FileBrowser.Application.FileChanges;

public sealed class FileDeletedEventHandler : INotificationHandler<FileDeletedEvent>
{
    private readonly IFileSearchIndex _index;

    public FileDeletedEventHandler(IFileSearchIndex index)
    {
        _index = index;
    }

    public Task Handle(FileDeletedEvent notification, CancellationToken cancellationToken)
    {
        return _index.RemoveAsync(notification.Path, cancellationToken);
    }
}
