using MediatR;

namespace FileBrowser.Application.FileChanges;

public sealed record FileCreatedEvent(string Path) : INotification;
