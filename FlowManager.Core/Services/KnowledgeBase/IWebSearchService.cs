using FlowManager.Core.Models.KnowledgeBase;

namespace FlowManager.Core.Services.KnowledgeBase;

/// <summary>
/// Service for performing web searches using external APIs
/// </summary>
public interface IWebSearchService
{
    Task<List<WebSearchResult>> SearchWebAsync(string query, int maxResults = 5);

    bool IsConfigured();
}
