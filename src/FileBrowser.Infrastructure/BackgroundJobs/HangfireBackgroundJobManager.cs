using FileBrowser.Application.Abtractstions.BackgroundJobs;
using Hangfire;
using System.Linq.Expressions;

namespace FileBrowser.Infrastructure.BackgroundJobs;

public class HangfireBackgroundJobManager(IBackgroundJobClient backgroundJobClient) : IBackgroundJobManager
{
    public string Enqueue<T>(Expression<Func<T, Task>> job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return backgroundJobClient.Enqueue(job);
    }
}
