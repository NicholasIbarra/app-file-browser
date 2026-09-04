using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

internal interface ISampleDataDestination
{
    string Description { get; }
    Task InitializeAsync();
    Task CreateFolderAsync(string relativePath);
    Task<IReadOnlyCollection<string>> GetEntryNamesAsync(string relativePath);
    Task WriteTextAsync(string relativePath, string content);
}

internal static class SampleDataDestination
{
    public static ISampleDataDestination Create(SampleDataOptions options) => options.Provider switch
    {
        SampleDataProvider.Local => new LocalSampleDataDestination(ResolveOutputPath(options.OutputPath)),
        SampleDataProvider.Azure => new AzureSampleDataDestination(options.Azure),
        _ => throw new NotSupportedException($"Unknown sample data provider '{options.Provider}'.")
    };

    private static string ResolveOutputPath(string configuredPath)
    {
        if (Path.IsPathFullyQualified(configuredPath)) return Path.GetFullPath(configuredPath);
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FileBrowser.slnx")))
                return Path.GetFullPath(configuredPath, directory.FullName);
        }
        return Path.GetFullPath(configuredPath, Directory.GetCurrentDirectory());
    }
}

internal sealed class LocalSampleDataDestination(string rootPath) : ISampleDataDestination
{
    public string Description => rootPath;
    public Task InitializeAsync() { Directory.CreateDirectory(rootPath); return Task.CompletedTask; }
    public Task CreateFolderAsync(string relativePath) { Directory.CreateDirectory(GetPath(relativePath)); return Task.CompletedTask; }
    public Task<IReadOnlyCollection<string>> GetEntryNamesAsync(string relativePath) => Task.FromResult<IReadOnlyCollection<string>>(
        Directory.EnumerateFileSystemEntries(GetPath(relativePath)).Select(Path.GetFileName).OfType<string>().ToArray());
    public Task WriteTextAsync(string relativePath, string content) =>
        File.WriteAllTextAsync(GetPath(relativePath), content, Encoding.UTF8);
    private string GetPath(string relativePath) => Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
}

internal sealed class AzureSampleDataDestination : ISampleDataDestination
{
    private readonly BlobContainerClient _container;
    private readonly string _prefix;
    private readonly bool _createContainer;

    public AzureSampleDataDestination(AzureSampleDataOptions options)
    {
        _container = new BlobContainerClient(options.ConnectionString, options.ContainerName);
        _prefix = string.IsNullOrWhiteSpace(options.Prefix) ? string.Empty : options.Prefix.Replace('\\', '/').Trim('/') + "/";
        _createContainer = options.CreateContainerIfMissing;
    }

    public string Description => $"{_container.Uri}/{_prefix}";

    public async Task InitializeAsync()
    {
        if (_createContainer) await _container.CreateIfNotExistsAsync();
        else if (!await _container.ExistsAsync()) throw new InvalidOperationException($"Azure container '{_container.Name}' does not exist.");
    }

    public async Task CreateFolderAsync(string relativePath)
    {
        using var empty = new MemoryStream();
        await _container.GetBlobClient(ToBlobName(relativePath.TrimEnd('/') + "/")).UploadAsync(empty, overwrite: true);
    }

    public async Task<IReadOnlyCollection<string>> GetEntryNamesAsync(string relativePath)
    {
        var prefix = string.IsNullOrEmpty(relativePath) ? _prefix : ToBlobName(relativePath.Trim('/') + "/");
        var names = new List<string>();
        await foreach (var item in _container.GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, "/", prefix, CancellationToken.None))
        {
            if (item.IsPrefix)
                names.Add(item.Prefix.TrimEnd('/').Split('/')[^1]);
            else if (item.IsBlob && !item.Blob.Name.EndsWith('/'))
                names.Add(item.Blob.Name[(item.Blob.Name.LastIndexOf('/') + 1)..]);
        }
        return names;
    }

    public async Task WriteTextAsync(string relativePath, string content)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await _container.GetBlobClient(ToBlobName(relativePath)).UploadAsync(stream, overwrite: true);
    }

    private string ToBlobName(string relativePath) => _prefix + relativePath.Replace('\\', '/').TrimStart('/');
}
