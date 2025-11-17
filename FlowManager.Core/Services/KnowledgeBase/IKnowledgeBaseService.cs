using FlowManager.Core.Models.KnowledgeBase;

namespace FlowManager.Core.Services.KnowledgeBase;

/// <summary>
/// Service for managing knowledge base documents and semantic search
/// </summary>
public interface IKnowledgeBaseService
{
    Task<KnowledgeDocument> UploadDocumentAsync(Stream fileStream, string fileName, DocumentCategory category, Dictionary<string, string>? metadata = null);

    Task<List<DocumentChunk>> ProcessDocumentAsync(Guid documentId);

    Task<List<DocumentChunk>> SearchKnowledgeBaseAsync(string query, int topK = 5, float minScore = 0.0f);

    Task<bool> DeleteDocumentAsync(Guid documentId);

    Task<List<KnowledgeDocument>> GetAllDocumentsAsync();

    Task<KnowledgeDocument?> GetDocumentAsync(Guid documentId);

    Task<float[]> GenerateEmbeddingAsync(string text);

    Task ReindexAllDocumentsAsync();
}
