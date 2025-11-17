namespace FlowManager.Core.Models.KnowledgeBase;

/// <summary>
/// Represents a chunk of text extracted from a document with its embedding
/// </summary>
public class DocumentChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocumentId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public int? PageNumber { get; set; }

    public string? SectionTitle { get; set; }

    public float[] Embedding { get; set; } = Array.Empty<float>();

    public int TokenCount { get; set; }
}
