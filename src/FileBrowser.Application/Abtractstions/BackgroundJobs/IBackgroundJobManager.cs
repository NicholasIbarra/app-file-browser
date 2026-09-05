using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace FileBrowser.Application.Abtractstions.BackgroundJobs;

public interface IBackgroundJobManager
{
    string Enqueue<T>(Expression<Func<T, Task>> job);
}
