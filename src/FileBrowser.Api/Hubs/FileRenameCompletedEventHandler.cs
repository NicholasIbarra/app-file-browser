using FileBrowser.Application.Files.Events;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace FileBrowser.Api.Hubs;

public sealed class FileRenameCompletedEventHandler(
    IHubContext<FileHub, IFileHubClient> hubContext) : INotificationHandler<FileRenamedEvent>
{
    public Task Handle(FileRenamedEvent notification, CancellationToken cancellationToken)
    {
        return hubContext.Clients.All.FileRenameCompleted(notification.SourcePath, notification.NewName);
    }
}
