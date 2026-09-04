namespace FileBrowser.Infrastructure.FileSystem;

public sealed class FileBrowserOptions
{
    public const string SectionName = "FileBrowser";

    public FileSystemProvider Provider { get; set; } = FileSystemProvider.Local;

    public string HomeDirectory { get; set; } = "./local-storage";

    public bool CreateIfMissing { get; set; } = true;

    public AzureFileSystemOptions Azure { get; set; } = new();
}

public sealed class AzureFileSystemOptions
{
    public string? ConnectionString { get; set; }

    public string ContainerName { get; set; } = string.Empty;

    public string? Prefix { get; set; }
}
