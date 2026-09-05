using Microsoft.Extensions.AI;

namespace FileBrowser.Application.Search.Prompts;

public interface IFileSearchPromptBuilder
{
    IReadOnlyList<ChatMessage> BuildQueryNormalizationPrompt(string query);

    IReadOnlyList<ChatMessage> BuildResultsSummaryPrompt(string query, IReadOnlyList<FileSearchResultDto> results);
}
