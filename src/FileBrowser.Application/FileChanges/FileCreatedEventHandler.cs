using FileBrowser.Application.Abtractstions.Indexing;
using MediatR;

namespace FileBrowser.Application.FileChanges;

public sealed class FileCreatedEventHandler : INotificationHandler<FileCreatedEvent>
{
    private readonly IFileSearchIndex _index;

    public FileCreatedEventHandler(IFileSearchIndex index)
    {
        _index = index;
    }

    public Task Handle(FileCreatedEvent notification, CancellationToken cancellationToken)
    {
        return _index.AddOrUpdateAsync(notification.Path, cancellationToken);
    }
}
