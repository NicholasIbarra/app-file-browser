using FileBrowser.Application.Abtractstions.Indexing;
using Microsoft.Extensions.Hosting;

namespace FileBrowser.Infrastructure.BackgroundServices;

public class FileIndexHostedService : BackgroundService
{
    private readonly IFileSearchIndex _index;

    public FileIndexHostedService(IFileSearchIndex index)
    {
        _index = index;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _index.RebuildAsync(stoppingToken);
    }
}
