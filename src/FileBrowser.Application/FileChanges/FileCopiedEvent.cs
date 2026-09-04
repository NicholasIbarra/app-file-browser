using MediatR;

namespace FileBrowser.Application.FileChanges;

public sealed record FileCopiedEvent(string DestinationPath) : INotification;
