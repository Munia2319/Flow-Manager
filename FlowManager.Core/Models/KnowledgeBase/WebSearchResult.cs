namespace FlowManager.Core.Models.KnowledgeBase;

/// <summary>
/// Result from web search API
/// </summary>
public class WebSearchResult
{
    public string Title { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Snippet { get; set; } = string.Empty;

    public string? FullContent { get; set; }

    public DateTime? PublishedDate { get; set; }
}
