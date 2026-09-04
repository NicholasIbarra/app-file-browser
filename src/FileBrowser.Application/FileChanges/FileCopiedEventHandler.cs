using FileBrowser.Application.Abtractstions.Indexing;
using MediatR;

namespace FileBrowser.Application.FileChanges;

public sealed class FileCopiedEventHandler : INotificationHandler<FileCopiedEvent>
{
    private readonly IFileSearchIndex _index;

    public FileCopiedEventHandler(IFileSearchIndex index)
    {
        _index = index;
    }

    public Task Handle(FileCopiedEvent notification, CancellationToken cancellationToken)
    {
        return _index.AddOrUpdateAsync(notification.DestinationPath, cancellationToken);
    }
}
