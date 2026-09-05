using FileBrowser.Application.Abtractstions.AI;
using OpenAI.Embeddings;
using static System.Net.Mime.MediaTypeNames;

namespace FileBrowser.Infrastructure.AI;

public sealed class OpenAiEmbeddingService(EmbeddingClient client) : IEmbeddingService
{
    public string ModelName => "text-embedding-3-small";
    public string ModelVersion => "1";

    public async Task<IReadOnlyDictionary<string, float[]>> GenerateAsync(
        IReadOnlyCollection<string> inputs,
        CancellationToken cancellationToken = default)
    {
        var inputList = inputs.ToArray();

        var result = await client.GenerateEmbeddingsAsync(
            inputList,
            cancellationToken: cancellationToken);

        return inputList
            .Zip(
                result.Value,
                (input, embedding) => new
                {
                    Input = input,
                    Embedding = embedding.ToFloats().ToArray()
                })
            .ToDictionary(
                x => x.Input,
                x => x.Embedding);
    }

    public async Task<float[]> GenerateAsync(string input, CancellationToken cancellationToken = default)
    {
        var result = await client.GenerateEmbeddingAsync(input, cancellationToken: cancellationToken);

        return result.Value.ToFloats().ToArray();
    }
}
