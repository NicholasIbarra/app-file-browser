using System;
using System.Collections.Generic;
using System.Text;

namespace FileBrowser.Application.Abtractstions.AI;

public interface IEmbeddingService
{
    public string ModelName { get; }

    public string ModelVersion { get; }

    public Task<IReadOnlyDictionary<string, float[]>> GenerateAsync(
        IReadOnlyCollection<string> inputs,
        CancellationToken cancellationToken = default);
}
