using System;
using System.Collections.Generic;
using System.Text;

namespace FileBrowser.Infrastructure.FileSystem;

public sealed class FileBrowserOptions
{
    public const string SectionName = "FileBrowser";

    public required string HomeDirectory { get; init; }
}
