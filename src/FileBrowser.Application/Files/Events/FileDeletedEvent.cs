using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace FileBrowser.Application.Files.Events;

public sealed record FileDeletedEvent(string Path) : INotification;
