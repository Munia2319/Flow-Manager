namespace FlowManager.Core.Models.KnowledgeBase;

/// <summary>
/// Represents a document in the knowledge base
/// </summary>
public class KnowledgeDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public DocumentCategory Category { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadDate { get; set; } = DateTime.UtcNow;

    public long FileSize { get; set; }

    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;

    public int ChunkCount { get; set; }

    public Dictionary<string, string> Metadata { get; set; } = new();

    public string? ErrorMessage { get; set; }
}
