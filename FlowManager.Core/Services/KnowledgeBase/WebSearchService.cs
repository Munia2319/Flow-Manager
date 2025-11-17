using FlowManager.Core.Models.KnowledgeBase;
using Newtonsoft.Json;
using System.Text;
using System.Web;

namespace FlowManager.Core.Services.KnowledgeBase;

/// <summary>
/// Service for performing web searches using Bing Search API
/// </summary>
public class WebSearchService : IWebSearchService
{
    private readonly HttpClient _httpClient;
    private readonly string? _bingApiKey;
    private readonly string _bingEndpoint = "https://api.bing.microsoft.com/v7.0/search";

    public WebSearchService(string? bingApiKey = null)
    {
        _bingApiKey = bingApiKey;
        _httpClient = new HttpClient();
    }

    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(_bingApiKey);
    }

    public async Task<List<WebSearchResult>> SearchWebAsync(string query, int maxResults = 5)
    {
        if (!IsConfigured())
        {
            // Fallback: return mock results or throw exception
            return new List<WebSearchResult>
            {
                new WebSearchResult
                {
                    Title = "Web Search Not Configured",
                    Snippet = "Bing API key is not configured. Please add your Bing Search API key to appsettings.json to enable web search functionality.",
                    Url = "https://portal.azure.com"
                }
            };
        }

        try
        {
            var encodedQuery = HttpUtility.UrlEncode(query);
            var requestUrl = $"{_bingEndpoint}?q={encodedQuery}&count={maxResults}";

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _bingApiKey);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Bing API error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var searchResponse = JsonConvert.DeserializeObject<BingSearchResponse>(responseContent);

            if (searchResponse?.WebPages?.Value == null)
            {
                return new List<WebSearchResult>();
            }

            return searchResponse.WebPages.Value
                .Select(page => new WebSearchResult
                {
                    Title = page.Name ?? "Untitled",
                    Url = page.Url ?? "",
                    Snippet = page.Snippet ?? "",
                    PublishedDate = ParseDate(page.DateLastCrawled)
                })
                .ToList();
        }
        catch (Exception ex)
        {
            // Log error and return empty results
            Console.WriteLine($"Web search error: {ex.Message}");
            return new List<WebSearchResult>
            {
                new WebSearchResult
                {
                    Title = "Search Error",
                    Snippet = $"An error occurred while searching the web: {ex.Message}",
                    Url = ""
                }
            };
        }
    }

    private DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return null;
        }

        if (DateTime.TryParse(dateString, out var date))
        {
            return date;
        }

        return null;
    }

    // Bing API response models
    private class BingSearchResponse
    {
        public WebPages? WebPages { get; set; }
    }

    private class WebPages
    {
        public List<WebPage>? Value { get; set; }
    }

    private class WebPage
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? Snippet { get; set; }
        public string? DateLastCrawled { get; set; }
    }
}
