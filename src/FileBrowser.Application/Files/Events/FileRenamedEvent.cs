using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace FileBrowser.Application.Files.Events;

public record FileRenamedEvent(string SourcePath, string NewName) : INotification;
