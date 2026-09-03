namespace FileBrowser.Infrastructure.FileSystem;

public sealed class FileBrowserOptions
{
    public const string SectionName = "FileBrowser";

    public FileSystemProvider Provider { get; set; } = FileSystemProvider.Local;

    public string HomeDirectory { get; set; } = "./local-storage";

    public bool CreateIfMissing { get; set; } = true;
}
