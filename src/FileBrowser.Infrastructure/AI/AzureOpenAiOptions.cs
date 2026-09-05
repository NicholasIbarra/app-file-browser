namespace FileBrowser.Infrastructure.AI;

public sealed class AzureOpenAiOptions
{
    public const string SectionName = "OpenAI:AzureOpenAI";

    public string Key { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public string ChatModel { get; set; } = "gpt-5-chat";

    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    public bool Enabled { get; set; } = false;
}
