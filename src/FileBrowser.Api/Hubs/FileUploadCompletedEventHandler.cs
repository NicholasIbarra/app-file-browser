using FileBrowser.Application.FileChanges;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace FileBrowser.Api.Hubs;

public sealed class FileUploadCompletedEventHandler(
    IHubContext<FileHub, IFileHubClient> hubContext) : INotificationHandler<FileCreatedEvent>
{
    public Task Handle(FileCreatedEvent notification, CancellationToken cancellationToken)
    {
        return hubContext.Clients.All.FileUploadCompleted(notification.Path);
    }
}
