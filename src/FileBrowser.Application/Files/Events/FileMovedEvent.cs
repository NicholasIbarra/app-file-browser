using MediatR;

namespace FileBrowser.Application.Files.Events;

public sealed record FileMovedEvent(
    string SourcePath,
    string DestinationPath) : INotification;
