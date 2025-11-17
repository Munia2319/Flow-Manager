using FlowManager.Core.Models.KnowledgeBase;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace FlowManager.Core.Services.KnowledgeBase;

/// <summary>
/// Service for managing knowledge base documents and semantic search
/// </summary>
public class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly string _basePath;
    private readonly string _documentsPath;
    private readonly string _metadataPath;
    private readonly string _chunksPath;
    private readonly string _embeddingsPath;
    private readonly HttpClient _httpClient;
    private readonly string _openAiApiKey;
    private readonly Dictionary<Guid, DocumentChunk> _chunkIndex;
    private readonly SemaphoreSlim _indexLock = new(1, 1);

    public KnowledgeBaseService(string basePath, string openAiApiKey)
    {
        _basePath = basePath;
        _documentsPath = Path.Combine(basePath, "Documents");
        _metadataPath = Path.Combine(basePath, "Metadata");
        _chunksPath = Path.Combine(basePath, "Chunks");
        _embeddingsPath = Path.Combine(basePath, "Embeddings");
        _openAiApiKey = openAiApiKey;
        _httpClient = new HttpClient();
        _chunkIndex = new Dictionary<Guid, DocumentChunk>();

        // Create directories
        Directory.CreateDirectory(_documentsPath);
        Directory.CreateDirectory(_metadataPath);
        Directory.CreateDirectory(_chunksPath);
        Directory.CreateDirectory(_embeddingsPath);

        // Load index
        Task.Run(LoadIndexAsync).Wait();
    }

    public async Task<KnowledgeDocument> UploadDocumentAsync(
        Stream fileStream,
        string fileName,
        DocumentCategory category,
        Dictionary<string, string>? metadata = null)
    {
        var document = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            Title = Path.GetFileNameWithoutExtension(fileName),
            OriginalFileName = fileName,
            Category = category,
            UploadDate = DateTime.UtcNow,
            Status = ProcessingStatus.Pending,
            Metadata = metadata ?? new Dictionary<string, string>()
        };

        // Save file
        var extension = Path.GetExtension(fileName);
        var filePath = Path.Combine(_documentsPath, $"{document.Id}{extension}");
        document.FilePath = filePath;

        using (var fs = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fs);
            document.FileSize = fs.Length;
        }

        // Save metadata
        await SaveDocumentMetadataAsync(document);

        // Start processing in background
        _ = Task.Run(async () =>
        {
            try
            {
                await ProcessDocumentAsync(document.Id);
            }
            catch (Exception ex)
            {
                document.Status = ProcessingStatus.Failed;
                document.ErrorMessage = ex.Message;
                await SaveDocumentMetadataAsync(document);
            }
        });

        return document;
    }

    public async Task<List<DocumentChunk>> ProcessDocumentAsync(Guid documentId)
    {
        var document = await GetDocumentAsync(documentId);
        if (document == null)
        {
            throw new FileNotFoundException("Document not found");
        }

        document.Status = ProcessingStatus.Processing;
        await SaveDocumentMetadataAsync(document);

        try
        {
            // Extract text
            var text = await ExtractTextAsync(document.FilePath);

            // Chunk text
            var chunks = ChunkText(text, documentId);

            // Generate embeddings
            foreach (var chunk in chunks)
            {
                chunk.Embedding = await GenerateEmbeddingAsync(chunk.Content);
                await Task.Delay(100); // Rate limiting
            }

            // Save chunks
            await SaveChunksAsync(documentId, chunks);

            // Update document
            document.ChunkCount = chunks.Count;
            document.Status = ProcessingStatus.Indexed;
            await SaveDocumentMetadataAsync(document);

            // Update index
            await _indexLock.WaitAsync();
            try
            {
                foreach (var chunk in chunks)
                {
                    _chunkIndex[chunk.Id] = chunk;
                }
            }
            finally
            {
                _indexLock.Release();
            }

            return chunks;
        }
        catch (Exception ex)
        {
            document.Status = ProcessingStatus.Failed;
            document.ErrorMessage = ex.Message;
            await SaveDocumentMetadataAsync(document);
            throw;
        }
    }

    public async Task<List<DocumentChunk>> SearchKnowledgeBaseAsync(string query, int topK = 5, float minScore = 0.0f)
    {
        // Generate query embedding
        var queryEmbedding = await GenerateEmbeddingAsync(query);

        // Search in index
        await _indexLock.WaitAsync();
        List<(DocumentChunk chunk, float score)> results;
        try
        {
            results = _chunkIndex.Values
                .Select(chunk => (chunk, score: CosineSimilarity(queryEmbedding, chunk.Embedding)))
                .Where(x => x.score >= minScore)
                .OrderByDescending(x => x.score)
                .Take(topK)
                .ToList();
        }
        finally
        {
            _indexLock.Release();
        }

        return results.Select(x => x.chunk).ToList();
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentId)
    {
        var document = await GetDocumentAsync(documentId);
        if (document == null) return false;

        // Delete file
        if (File.Exists(document.FilePath))
        {
            File.Delete(document.FilePath);
        }

        // Delete chunks
        var chunksFile = Path.Combine(_chunksPath, $"{documentId}_chunks.json");
        if (File.Exists(chunksFile))
        {
            File.Delete(chunksFile);
        }

        // Remove from index
        await _indexLock.WaitAsync();
        try
        {
            var chunkIds = _chunkIndex.Values
                .Where(c => c.DocumentId == documentId)
                .Select(c => c.Id)
                .ToList();

            foreach (var id in chunkIds)
            {
                _chunkIndex.Remove(id);
            }
        }
        finally
        {
            _indexLock.Release();
        }

        // Update metadata file
        var allDocs = await GetAllDocumentsAsync();
        allDocs.RemoveAll(d => d.Id == documentId);
        await SaveAllDocumentsMetadataAsync(allDocs);

        return true;
    }

    public async Task<List<KnowledgeDocument>> GetAllDocumentsAsync()
    {
        var metadataFile = Path.Combine(_metadataPath, "documents.json");
        if (!File.Exists(metadataFile))
        {
            return new List<KnowledgeDocument>();
        }

        var json = await File.ReadAllTextAsync(metadataFile);
        return JsonConvert.DeserializeObject<List<KnowledgeDocument>>(json) ?? new List<KnowledgeDocument>();
    }

    public async Task<KnowledgeDocument?> GetDocumentAsync(Guid documentId)
    {
        var allDocs = await GetAllDocumentsAsync();
        return allDocs.FirstOrDefault(d => d.Id == documentId);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var request = new
        {
            input = text,
            model = "text-embedding-3-small"
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
        requestMessage.Headers.Add("Authorization", $"Bearer {_openAiApiKey}");
        requestMessage.Content = new StringContent(
            JsonConvert.SerializeObject(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();

        #pragma warning disable CS8602 // Dynamic types from JSON deserialization
        dynamic? result = JsonConvert.DeserializeObject(responseContent);

        if (result == null || result.data == null || result.data.Count == 0)
        {
            throw new Exception("No embedding returned from API");
        }

        // After the null check above, we know result.data is not null
        var embeddingArray = result.data[0].embedding;
        var embedding = new List<float>();
        foreach (var value in embeddingArray)
        {
            embedding.Add((float)value);
        }
        #pragma warning restore CS8602

        return embedding.ToArray();
    }

    public async Task ReindexAllDocumentsAsync()
    {
        var documents = await GetAllDocumentsAsync();

        await _indexLock.WaitAsync();
        try
        {
            _chunkIndex.Clear();
        }
        finally
        {
            _indexLock.Release();
        }

        foreach (var doc in documents.Where(d => d.Status == ProcessingStatus.Indexed))
        {
            var chunks = await LoadChunksAsync(doc.Id);

            await _indexLock.WaitAsync();
            try
            {
                foreach (var chunk in chunks)
                {
                    _chunkIndex[chunk.Id] = chunk;
                }
            }
            finally
            {
                _indexLock.Release();
            }
        }
    }

    // Helper methods

    private async Task<string> ExtractTextAsync(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        switch (extension)
        {
            case ".txt":
            case ".md":
                return await File.ReadAllTextAsync(filePath);

            case ".pdf":
                // For now, return a placeholder
                // In production, use a library like iTextSharp or PdfPig
                return "PDF text extraction requires additional library. Placeholder text.";

            case ".docx":
                // For now, return a placeholder
                // In production, use DocumentFormat.OpenXml
                return "DOCX text extraction requires additional library. Placeholder text.";

            default:
                throw new NotSupportedException($"File type {extension} is not supported");
        }
    }

    private List<DocumentChunk> ChunkText(string text, Guid documentId)
    {
        var chunks = new List<DocumentChunk>();
        const int chunkSize = 800; // words
        const int overlap = 100; // words

        var words = Regex.Split(text, @"\s+");
        var chunkIndex = 0;

        for (int i = 0; i < words.Length; i += (chunkSize - overlap))
        {
            var chunkWords = words.Skip(i).Take(chunkSize).ToArray();
            var chunkText = string.Join(" ", chunkWords);

            if (string.IsNullOrWhiteSpace(chunkText)) continue;

            chunks.Add(new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                ChunkIndex = chunkIndex++,
                Content = chunkText,
                TokenCount = chunkWords.Length
            });
        }

        return chunks;
    }

    private float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Vectors must have the same length");
        }

        float dotProduct = 0;
        float magnitudeA = 0;
        float magnitudeB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        magnitudeA = (float)Math.Sqrt(magnitudeA);
        magnitudeB = (float)Math.Sqrt(magnitudeB);

        if (magnitudeA == 0 || magnitudeB == 0)
        {
            return 0;
        }

        return dotProduct / (magnitudeA * magnitudeB);
    }

    private async Task SaveDocumentMetadataAsync(KnowledgeDocument document)
    {
        var allDocs = await GetAllDocumentsAsync();
        var existing = allDocs.FirstOrDefault(d => d.Id == document.Id);

        if (existing != null)
        {
            allDocs.Remove(existing);
        }

        allDocs.Add(document);
        await SaveAllDocumentsMetadataAsync(allDocs);
    }

    private async Task SaveAllDocumentsMetadataAsync(List<KnowledgeDocument> documents)
    {
        var metadataFile = Path.Combine(_metadataPath, "documents.json");
        var json = JsonConvert.SerializeObject(documents, Formatting.Indented);
        await File.WriteAllTextAsync(metadataFile, json);
    }

    private async Task SaveChunksAsync(Guid documentId, List<DocumentChunk> chunks)
    {
        var chunksFile = Path.Combine(_chunksPath, $"{documentId}_chunks.json");
        var json = JsonConvert.SerializeObject(chunks, Formatting.Indented);
        await File.WriteAllTextAsync(chunksFile, json);
    }

    private async Task<List<DocumentChunk>> LoadChunksAsync(Guid documentId)
    {
        var chunksFile = Path.Combine(_chunksPath, $"{documentId}_chunks.json");
        if (!File.Exists(chunksFile))
        {
            return new List<DocumentChunk>();
        }

        var json = await File.ReadAllTextAsync(chunksFile);
        return JsonConvert.DeserializeObject<List<DocumentChunk>>(json) ?? new List<DocumentChunk>();
    }

    private async Task LoadIndexAsync()
    {
        var documents = await GetAllDocumentsAsync();

        foreach (var doc in documents.Where(d => d.Status == ProcessingStatus.Indexed))
        {
            var chunks = await LoadChunksAsync(doc.Id);

            await _indexLock.WaitAsync();
            try
            {
                foreach (var chunk in chunks)
                {
                    _chunkIndex[chunk.Id] = chunk;
                }
            }
            finally
            {
                _indexLock.Release();
            }
        }
    }
}
