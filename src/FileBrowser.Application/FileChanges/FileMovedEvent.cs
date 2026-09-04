using MediatR;

namespace FileBrowser.Application.FileChanges;

public sealed record FileMovedEvent(
    string SourcePath,
    string DestinationPath) : INotification;
