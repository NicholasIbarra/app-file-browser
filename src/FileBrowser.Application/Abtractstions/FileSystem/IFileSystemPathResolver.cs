using System;
using System.Collections.Generic;
using System.Text;

namespace FileBrowser.Application.Abtractstions.FileSystem;

public interface IFileSystemPathResolver
{
    string Resolve(string? path);
    string ToRelativePath(string physicalPath);
}
