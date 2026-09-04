using System;
using System.Collections.Generic;
using System.Text;

using MediatR;

namespace FileBrowser.Application.FileChanges;

public sealed record FileDeletedEvent(string Path) : INotification;
