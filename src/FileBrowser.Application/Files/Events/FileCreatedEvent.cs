using MediatR;

namespace FileBrowser.Application.Files.Events;

public sealed record FileCreatedEvent(string Path) : INotification;
