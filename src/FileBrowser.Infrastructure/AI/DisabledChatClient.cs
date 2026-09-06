using Microsoft.Extensions.AI;

namespace FileBrowser.Infrastructure.AI;

/// <summary>
/// Registered in place of the Azure OpenAI-backed IChatClient when
/// OpenAI:AzureOpenAI:Enabled is false, so no Azure OpenAI client needs to be
/// constructed. Callers already avoid semantic search when no indexed entries
/// have embeddings, so this should never actually be invoked.
/// </summary>
public sealed class DisabledChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "Chat completion was requested but OpenAI:AzureOpenAI:Enabled is false.");

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "Chat completion was requested but OpenAI:AzureOpenAI:Enabled is false.");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
