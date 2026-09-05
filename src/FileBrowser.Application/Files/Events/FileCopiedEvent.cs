using MediatR;

namespace FileBrowser.Application.Files.Events;

public sealed record FileCopiedEvent(string DestinationPath) : INotification;
