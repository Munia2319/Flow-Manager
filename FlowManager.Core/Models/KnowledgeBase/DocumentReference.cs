namespace FlowManager.Core.Models.KnowledgeBase;

/// <summary>
/// Reference to a source document
/// </summary>
public class DocumentReference
{
    public string Title { get; set; } = string.Empty;

    public int? PageNumber { get; set; }

    public string? SectionTitle { get; set; }

    public Guid? DocumentId { get; set; }

    public string? Url { get; set; }

    public float RelevanceScore { get; set; }
}
