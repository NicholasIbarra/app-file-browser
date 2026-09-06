using FileBrowser.Application.Abtractstions.AI;

namespace FileBrowser.Infrastructure.AI;

/// <summary>
/// Registered in place of <see cref="OpenAiEmbeddingService"/> when
/// OpenAI:AzureOpenAI:Enabled is false, so no Azure OpenAI client needs to be
/// constructed. Callers already check AzureOpenAiOptions.Enabled before
/// generating embeddings, so this should never actually be invoked.
/// </summary>
public sealed class DisabledEmbeddingService : IEmbeddingService
{
    public string ModelName => "disabled";
    public string ModelVersion => "0";

    public Task<float[]> GenerateAsync(string input, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "Embedding generation was requested but OpenAI:AzureOpenAI:Enabled is false.");

    public Task<IReadOnlyDictionary<string, float[]>> GenerateAsync(
        IReadOnlyCollection<string> inputs,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "Embedding generation was requested but OpenAI:AzureOpenAI:Enabled is false.");
}
