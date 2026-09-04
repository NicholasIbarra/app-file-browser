internal sealed class SampleDataOptions
{
    public string OutputPath { get; init; } = "src/FileBrowser.Api/local-storage";
    public int TotalFiles { get; init; } = 10_000;
    public int TotalFolders { get; init; } = 500;
    public int MinFilesPerFolder { get; init; } = 5;
    public int MaxFilesPerFolder { get; init; } = 40;
    public int MinFolderDepth { get; init; } = 2;
    public int MaxFolderDepth { get; init; } = 6;
    public int? Seed { get; init; }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(OutputPath);
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
