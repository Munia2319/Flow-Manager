using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FlowManager.Core.Models;
using FlowManager.Core.Models.KnowledgeBase;
using FlowManager.Core.Services.KnowledgeBase;

namespace FlowManager.Core.Nodes
{
    /// <summary>
    /// Search Department Node - Hybrid search system combining internal knowledge base + web search + AI summarization
    /// </summary>
    /// <remarks>
    /// <para>
    /// SearchDepartmentNode is an intelligent search system that automatically chooses the best information source
    /// based on query analysis, confidence scores, and time-sensitivity. It provides a seamless hybrid approach
    /// to information retrieval.
    /// </para>
    /// <para>
    /// <b>Search Strategy (Adaptive):</b>
    /// <list type="number">
    ///   <item><description><b>KB Confidence >= 0.75 + Not Time-Sensitive:</b> Use knowledge base only (fastest)</description></item>
    ///   <item><description><b>KB Confidence >= 0.5 OR Time-Sensitive:</b> Use hybrid (KB + Web)</description></item>
    ///   <item><description><b>KB Confidence &lt; 0.5:</b> Use web search only</description></item>
    ///   <item><description><b>No KB Available:</b> Fall back to model's general knowledge</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Key Features:</b>
    /// <list type="bullet">
    ///   <item><description><b>Semantic Search:</b> Uses embeddings for knowledge base similarity matching</description></item>
    ///   <item><description><b>Time-Sensitivity Detection:</b> Automatically identifies queries needing current information</description></item>
    ///   <item><description><b>Source Attribution:</b> Tracks and cites internal vs external sources</description></item>
    ///   <item><description><b>Confidence Scoring:</b> Provides transparency on search quality (0.0-1.0)</description></item>
    ///   <item><description><b>Conversation Context:</b> Uses chat history for follow-up questions</description></item>
    ///   <item><description><b>Custom Prompts:</b> Supports custom system prompts via config</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Configuration Parameters:</b>
    /// <list type="bullet">
    ///   <item><description><b>apiKey:</b> Required. OpenAI API key</description></item>
    ///   <item><description><b>systemPrompt:</b> Optional. Custom search/answer prompt (overrides adaptive prompts)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Outputs:</b>
    /// <list type="bullet">
    ///   <item><description><b>output:</b> The AI-generated answer with citations</description></item>
    ///   <item><description><b>searchResult:</b> Structured SearchResult object with metadata</description></item>
    ///   <item><description><b>source:</b> Search source used (KnowledgeBase, Hybrid, WebSearch, ModelKnowledge)</description></item>
    ///   <item><description><b>confidence:</b> Confidence score (0.0-1.0)</description></item>
    ///   <item><description><b>internalSources:</b> Count of internal KB sources used</description></item>
    ///   <item><description><b>externalSources:</b> Count of external web sources used</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Dependencies:</b>
    /// <br/>
    /// Requires IKnowledgeBaseService and IWebSearchService injected via constructor for full functionality.
    /// Works in degraded mode without these services (falls back to model knowledge).
    /// </para>
    /// </remarks>
    /// <example>
    /// JSON definition:
    /// <code>
    /// {
    ///   "id": "search_dept",
    ///   "type": "SearchDepartment",
    ///   "config": {
    ///     "apiKey": "sk-..."
    ///   }
    /// }
    /// </code>
    ///
    /// Usage pattern:
    /// <code>
    /// Input → Orchestrator → Search Department → Formatter
    ///
    /// // Adaptive behavior:
    /// // "What is our company policy?" → Knowledge Base (high confidence)
    /// // "Latest AI trends in 2025?" → Web Search (time-sensitive)
    /// // "Tesla stock price?" → Hybrid (medium KB confidence + finance data)
    /// </code>
    /// </example>
    public class SearchDepartmentNode : Node
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string OPENAI_API_URL = "https://api.openai.com/v1/chat/completions";

        private readonly IKnowledgeBaseService? _knowledgeBaseService;
        private readonly IWebSearchService? _webSearchService;

        public SearchDepartmentNode()
        {
            Type = "SearchDepartment";
        }

        public SearchDepartmentNode(IKnowledgeBaseService? knowledgeBaseService, IWebSearchService? webSearchService)
        {
            Type = "SearchDepartment";
            _knowledgeBaseService = knowledgeBaseService;
            _webSearchService = webSearchService;
        }
        
        public override async Task<Dictionary<string, object>> ExecuteAsync(
            Dictionary<string, object> inputs)
        {
            Status = NodeStatus.Running;
            StartTime = DateTime.UtcNow;
            
            try
            {
                // Get API key
                var apiKey = Config.ContainsKey("apiKey") 
                    ? Config["apiKey"]?.ToString() 
                    : throw new Exception("apiKey is required");
                
                // Get user question
                var userQuestion = inputs.ContainsKey("input")
                    ? inputs["input"]?.ToString() ?? ""
                    : "";

                // ✨ NEW: Hybrid Search Logic
                SearchSource searchSource = SearchSource.ModelKnowledge;
                float kbConfidence = 0.0f;
                List<DocumentChunk> kbResults = new();
                List<WebSearchResult> webResults = new();
                var internalSources = new List<DocumentReference>();
                var externalSources = new List<DocumentReference>();

                // STEP 1: Check if query is time-sensitive
                bool isTimeSensitive = IsTimeSensitiveQuery(userQuestion);

                // STEP 2: Search Knowledge Base
                if (_knowledgeBaseService != null)
                {
                    try
                    {
                        kbResults = await _knowledgeBaseService.SearchKnowledgeBaseAsync(userQuestion, topK: 5, minScore: 0.5f);

                        if (kbResults.Any())
                        {
                            // Calculate average confidence from top results
                            var embeddings = kbResults.Select(r => r.Embedding).ToArray();
                            var queryEmbedding = await _knowledgeBaseService.GenerateEmbeddingAsync(userQuestion);
                            var similarities = embeddings.Select(e => CosineSimilarity(queryEmbedding, e)).ToList();
                            kbConfidence = similarities.Any() ? similarities.Average() : 0.0f;

                            Console.WriteLine($"[SearchDepartment] 📚 KB Search: {kbResults.Count} results, confidence: {kbConfidence:F2}");

                            // Build internal source references
                            for (int i = 0; i < kbResults.Count; i++)
                            {
                                var chunk = kbResults[i];
                                var doc = await _knowledgeBaseService.GetDocumentAsync(chunk.DocumentId);
                                if (doc != null)
                                {
                                    internalSources.Add(new DocumentReference
                                    {
                                        Title = doc.Title,
                                        DocumentId = doc.Id,
                                        PageNumber = chunk.PageNumber,
                                        SectionTitle = chunk.SectionTitle,
                                        RelevanceScore = similarities[i]
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SearchDepartment] ⚠️ KB Search error: {ex.Message}");
                    }
                }

                // STEP 3: Decide search strategy based on confidence and time-sensitivity
                if (kbConfidence >= 0.75 && !isTimeSensitive)
                {
                    // High confidence: Use KB only
                    searchSource = SearchSource.KnowledgeBase;
                    Console.WriteLine($"[SearchDepartment] ✅ Using KB only (high confidence)");
                }
                else if (kbConfidence >= 0.5 || isTimeSensitive)
                {
                    // Medium confidence OR time-sensitive: Use hybrid approach
                    searchSource = SearchSource.Hybrid;
                    Console.WriteLine($"[SearchDepartment] 🔄 Using Hybrid approach (KB + Web)");

                    // Perform web search
                    if (_webSearchService != null && _webSearchService.IsConfigured())
                    {
                        try
                        {
                            webResults = await _webSearchService.SearchWebAsync(userQuestion, maxResults: 5);

                            foreach (var webResult in webResults)
                            {
                                externalSources.Add(new DocumentReference
                                {
                                    Title = webResult.Title,
                                    Url = webResult.Url,
                                    RelevanceScore = 0.0f // Web results don't have semantic scores
                                });
                            }

                            Console.WriteLine($"[SearchDepartment] 🌐 Web Search: {webResults.Count} results");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[SearchDepartment] ⚠️ Web Search error: {ex.Message}");
                        }
                    }
                }
                else if (kbConfidence < 0.5)
                {
                    // Low confidence: Use web search only
                    searchSource = SearchSource.WebSearch;
                    Console.WriteLine($"[SearchDepartment] 🌐 Using Web only (low KB confidence)");

                    if (_webSearchService != null && _webSearchService.IsConfigured())
                    {
                        try
                        {
                            webResults = await _webSearchService.SearchWebAsync(userQuestion, maxResults: 5);

                            foreach (var webResult in webResults)
                            {
                                externalSources.Add(new DocumentReference
                                {
                                    Title = webResult.Title,
                                    Url = webResult.Url
                                });
                            }

                            Console.WriteLine($"[SearchDepartment] 🌐 Web Search: {webResults.Count} results");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[SearchDepartment] ⚠️ Web Search error: {ex.Message}");
                        }
                    }
                }

                // STEP 4: Build system prompt based on search source
                // ✨ NEW: Check for custom system prompt in config
                string systemPrompt;
                if (Config.ContainsKey("systemPrompt") && !string.IsNullOrWhiteSpace(Config["systemPrompt"]?.ToString()))
                {
                    // Use custom system prompt from config
                    systemPrompt = Config["systemPrompt"]?.ToString() ?? "";
                    Console.WriteLine($"[SearchDepartment] 🎨 Using CUSTOM system prompt ({systemPrompt.Length} chars)");
                }
                else
                {
                    // Use default adaptive system prompt
                    systemPrompt = BuildSystemPrompt(searchSource, kbConfidence);
                    Console.WriteLine($"[SearchDepartment] 📋 Using DEFAULT system prompt (source: {searchSource})");
                }

                // STEP 5: Build context from search results
                var contextBuilder = new StringBuilder();

                if (searchSource == SearchSource.KnowledgeBase || searchSource == SearchSource.Hybrid)
                {
                    if (kbResults.Any())
                    {
                        contextBuilder.AppendLine("=== INTERNAL KNOWLEDGE BASE ===");
                        for (int i = 0; i < kbResults.Count; i++)
                        {
                            var chunk = kbResults[i];
                            var docRef = internalSources.ElementAtOrDefault(i);
                            contextBuilder.AppendLine($"\n[Source: {docRef?.Title ?? "Unknown"}, Page {chunk.PageNumber ?? 0}]");
                            contextBuilder.AppendLine(chunk.Content);
                        }
                        contextBuilder.AppendLine();
                    }
                }

                if (searchSource == SearchSource.WebSearch || searchSource == SearchSource.Hybrid)
                {
                    if (webResults.Any())
                    {
                        contextBuilder.AppendLine("=== EXTERNAL WEB SOURCES ===");
                        foreach (var webResult in webResults)
                        {
                            contextBuilder.AppendLine($"\n[Source: {webResult.Title}]");
                            contextBuilder.AppendLine($"URL: {webResult.Url}");
                            contextBuilder.AppendLine(webResult.Snippet);
                        }
                        contextBuilder.AppendLine();
                    }
                }

                // Build messages with conversation history
                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                // Include conversation history if available
                if (Context != null && Context.ConversationHistory != null && Context.ConversationHistory.Count > 0)
                {
                    Console.WriteLine($"[SearchDepartmentNode] 📚 Including {Context.ConversationHistory.Count} messages from conversation history");

                    foreach (var msg in Context.ConversationHistory)
                    {
                        messages.Add(new { role = msg.Role, content = msg.Content });
                    }
                }

                // Add context from search results
                if (contextBuilder.Length > 0)
                {
                    messages.Add(new { role = "user", content = $"Here is the context from available sources:\n\n{contextBuilder}" });
                }

                // Add the current user question
                messages.Add(new { role = "user", content = $"Question: {userQuestion}" });
                
                // API request
                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = messages,
                    temperature = 0.7
                };
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                
                var response = await _httpClient.PostAsJsonAsync(OPENAI_API_URL, requestBody);
                response.EnsureSuccessStatusCode();
                
                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(responseBody);
                
                var answer = jsonDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                // STEP 6: Build structured output with metadata
                var result = new SearchResult
                {
                    Source = searchSource,
                    Confidence = kbConfidence,
                    InternalSources = internalSources,
                    ExternalSources = externalSources,
                    Content = answer ?? "",
                    Summary = answer ?? "",
                    FallbackUsed = searchSource == SearchSource.WebSearch || searchSource == SearchSource.ModelKnowledge
                };

                Outputs["output"] = answer ?? "";
                Outputs["searchResult"] = result;
                Outputs["source"] = searchSource.ToString();
                Outputs["confidence"] = kbConfidence;
                Outputs["internalSources"] = internalSources.Count;
                Outputs["externalSources"] = externalSources.Count;

                Status = NodeStatus.Success;
                EndTime = DateTime.UtcNow;

                return Outputs;
            }
            catch (Exception ex)
            {
                Status = NodeStatus.Error;
                Error = ex.Message;
                EndTime = DateTime.UtcNow;
                throw;
            }
        }

        // Helper Methods

        private bool IsTimeSensitiveQuery(string query)
        {
            var timeSensitiveKeywords = new[]
            {
                "current", "latest", "today", "now", "recent", "2024", "2025",
                "this year", "this month", "new", "updated"
            };

            var lowerQuery = query.ToLowerInvariant();
            return timeSensitiveKeywords.Any(keyword => lowerQuery.Contains(keyword));
        }

        private string BuildSystemPrompt(SearchSource source, float confidence)
        {
            var basePrompt = @"## SYSTEM: Adaptive Company Search Assistant
You are a precise and adaptive search assistant that answers user questions using available information sources.
Your task is to return **clear, accurate, and verifiable answers**, adjusting response length and complexity to match the user's query.

### RULES:
- If the question is **simple**, return a **short 1–2 sentence answer**.
- If the question is **complex**, provide a **concise but informative 3–6 sentence summary**.
- **USE CONVERSATION CONTEXT**: If the user is asking a follow-up question, reference previous topics explicitly.
- **CITE SOURCES**: Always indicate whether information comes from internal knowledge base or external sources.
- Always answer in **natural language** (no lists or tables unless specifically requested).
";

            switch (source)
            {
                case SearchSource.KnowledgeBase:
                    return basePrompt + @"
### YOUR TASK:
You have access to our **internal company knowledge base**. Answer the question using ONLY the information provided from internal documents.
- Start with: ""According to our [document name]..."" or similar phrasing
- Include page numbers when available
- High confidence - the information is from our official company documents";

                case SearchSource.Hybrid:
                    return basePrompt + $@"
### YOUR TASK:
You have access to BOTH **internal company knowledge** (confidence: {confidence:F2}) AND **external web sources**.
- **Synthesize** information from both sources into ONE coherent answer
- Clearly distinguish which parts come from internal vs external sources
- Format: ""Our internal policy states... Additionally, industry standards recommend...""
- Prioritize internal information for company-specific questions
- Use external sources to supplement or provide broader context";

                case SearchSource.WebSearch:
                    return basePrompt + @"
### YOUR TASK:
Our internal knowledge base did not contain relevant information. You have access to **external web sources** only.
- Clearly state: ""Based on external sources (not in our knowledge base)...""
- Cite the source websites
- Note that this is general information, not company-specific";

                default:
                    return basePrompt + @"
### YOUR TASK:
Answer based on your general knowledge. Note that neither internal knowledge base nor web search results are available.";
            }
        }

        private float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
            {
                return 0.0f;
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
    }
}