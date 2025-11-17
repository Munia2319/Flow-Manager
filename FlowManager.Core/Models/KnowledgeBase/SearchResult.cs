namespace FlowManager.Core.Models.KnowledgeBase;

/// <summary>
/// Result from knowledge base or web search
/// </summary>
public class SearchResult
{
    public SearchSource Source { get; set; }

    public float Confidence { get; set; }

    public List<DocumentReference> InternalSources { get; set; } = new();

    public List<DocumentReference> ExternalSources { get; set; } = new();

    public string Content { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public bool FallbackUsed { get; set; }
}
