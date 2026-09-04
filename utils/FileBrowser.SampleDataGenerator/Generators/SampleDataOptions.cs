internal sealed class SampleDataOptions
{
    public SampleDataProvider Provider { get; init; } = SampleDataProvider.Local;
    public string OutputPath { get; init; } = "src/FileBrowser.Api/local-storage";
    public AzureSampleDataOptions Azure { get; init; } = new();
    public int TotalFiles { get; init; } = 10_000;
    public int TotalFolders { get; init; } = 500;
    public int MinFilesPerFolder { get; init; } = 5;
    public int MaxFilesPerFolder { get; init; } = 40;
    public int MinFolderDepth { get; init; } = 2;
    public int MaxFolderDepth { get; init; } = 6;
    public int? Seed { get; init; }

    public void Validate()
    {
        if (Provider == SampleDataProvider.Local)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(OutputPath);
        }
        else
        {
            Azure.Validate();
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(TotalFiles);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(TotalFolders);
        ArgumentOutOfRangeException.ThrowIfNegative(MinFilesPerFolder);
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxFilesPerFolder, MinFilesPerFolder);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MinFolderDepth);
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxFolderDepth, MinFolderDepth);

        if (TotalFolders < MinFolderDepth)
        {
            throw new InvalidOperationException("TotalFolders must be at least MinFolderDepth.");
        }

        var minimumFiles = (long)TotalFolders * MinFilesPerFolder;
        var maximumFiles = (long)TotalFolders * MaxFilesPerFolder;
        if (TotalFiles < minimumFiles || TotalFiles > maximumFiles)
        {
            throw new InvalidOperationException(
                $"TotalFiles must be between {minimumFiles:N0} and {maximumFiles:N0} for the configured folder count and per-folder limits.");
        }
    }
}

internal enum SampleDataProvider
{
    Local,
    Azure
}

internal sealed class AzureSampleDataOptions
{
    public string? ConnectionString { get; init; }
    public string ContainerName { get; init; } = string.Empty;
    public string? Prefix { get; init; }
    public bool CreateContainerIfMissing { get; init; }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ContainerName);
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException("SampleData:Azure:ConnectionString is required.");
        }
    }
}
