using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FlowManager.Core.Models;

namespace FlowManager.Core.Services
{
    /// <summary>
    /// Hybrid topic continuity detection service.
    /// Uses keyword-based detection for obvious changes, embeddings for subtle shifts.
    /// Helps identify when users switch topics vs continuing the same conversation.
    /// </summary>
    public class TopicContinuityDetector
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly double _keywordThreshold = 0.3;  // Below this = definitely different topic
        private readonly double _embeddingThreshold = 0.6; // Below this = likely different topic

        public TopicContinuityDetector(string apiKey)
        {
            _httpClient = new HttpClient();
            _apiKey = apiKey;
        }

        /// <summary>
        /// Detects topic continuity between current message and recent conversation.
        /// Returns a score from 0.0 (completely different) to 1.0 (same topic).
        /// Uses hybrid approach: keywords first, then embeddings if needed.
        /// </summary>
        public async Task<TopicContinuityResult> DetectContinuityAsync(
            string currentMessage,
            List<ConversationMessage> recentMessages)
        {
            if (recentMessages == null || recentMessages.Count == 0)
            {
                // First message - no comparison needed
                return new TopicContinuityResult
                {
                    SimilarityScore = 1.0,
                    IsTopicChange = false,
                    DetectionMethod = "first-message",
                    Reason = "First message in conversation"
                };
            }

            // Get recent user messages for comparison (ignore assistant responses)
            var recentUserMessages = recentMessages
                .Where(m => m.Role == "user")
                .TakeLast(3)
                .ToList();

            if (recentUserMessages.Count == 0)
            {
                return new TopicContinuityResult
                {
                    SimilarityScore = 1.0,
                    IsTopicChange = false,
                    DetectionMethod = "no-comparison",
                    Reason = "No previous user messages to compare"
                };
            }

            // STEP 1: Try keyword-based detection (fast, no API cost)
            var keywordScore = CalculateKeywordSimilarity(currentMessage, recentUserMessages);
            Console.WriteLine($"[TopicDetector] 🔑 Keyword similarity: {keywordScore:P0}");

            // If keyword score is very low, it's definitely a topic change
            if (keywordScore < _keywordThreshold)
            {
                Console.WriteLine($"[TopicDetector] ✅ Clear topic change detected via keywords");
                return new TopicContinuityResult
                {
                    SimilarityScore = keywordScore,
                    IsTopicChange = true,
                    DetectionMethod = "keyword",
                    Reason = $"Low keyword overlap ({keywordScore:P0})"
                };
            }

            // If keyword score is very high, it's definitely the same topic
            if (keywordScore > 0.7)
            {
                Console.WriteLine($"[TopicDetector] ✅ Same topic confirmed via keywords");
                return new TopicContinuityResult
                {
                    SimilarityScore = keywordScore,
                    IsTopicChange = false,
                    DetectionMethod = "keyword",
                    Reason = $"High keyword overlap ({keywordScore:P0})"
                };
            }

            // STEP 2: Ambiguous case - use embeddings for accurate detection
            Console.WriteLine($"[TopicDetector] 🤔 Ambiguous case (keyword score: {keywordScore:P0}), checking embeddings...");

            try
            {
                var embeddingScore = await CalculateEmbeddingSimilarityAsync(currentMessage, recentUserMessages);
                Console.WriteLine($"[TopicDetector] 🎯 Embedding similarity: {embeddingScore:P0}");

                var isTopicChange = embeddingScore < _embeddingThreshold;

                return new TopicContinuityResult
                {
                    SimilarityScore = embeddingScore,
                    IsTopicChange = isTopicChange,
                    DetectionMethod = "embedding",
                    Reason = isTopicChange
                        ? $"Topic change detected ({embeddingScore:P0} similarity)"
                        : $"Same topic ({embeddingScore:P0} similarity)"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TopicDetector] ⚠️ Embedding detection failed: {ex.Message}");
                Console.WriteLine($"[TopicDetector] 📉 Falling back to keyword score");

                // Fallback to keyword score if embeddings fail
                return new TopicContinuityResult
                {
                    SimilarityScore = keywordScore,
                    IsTopicChange = keywordScore < 0.5,
                    DetectionMethod = "keyword-fallback",
                    Reason = $"Embedding failed, using keywords ({keywordScore:P0})"
                };
            }
        }

        /// <summary>
        /// Fast keyword-based similarity detection.
        /// Extracts meaningful words and calculates Jaccard similarity.
        /// </summary>
        private double CalculateKeywordSimilarity(string current, List<ConversationMessage> recent)
        {
            var currentWords = ExtractKeywords(current);

            // Compare with recent messages
            var recentWords = new HashSet<string>();
            foreach (var msg in recent)
            {
                var words = ExtractKeywords(msg.Content);
                foreach (var word in words)
                    recentWords.Add(word);
            }

            if (currentWords.Count == 0 || recentWords.Count == 0)
                return 0.5; // Neutral score if can't extract keywords

            // Jaccard similarity: intersection / union
            var intersection = currentWords.Intersect(recentWords).Count();
            var union = currentWords.Union(recentWords).Count();

            return union > 0 ? (double)intersection / union : 0.0;
        }

        /// <summary>
        /// Extracts meaningful keywords from text.
        /// Filters out stopwords and short words.
        /// </summary>
        private HashSet<string> ExtractKeywords(string text)
        {
            var stopwords = new HashSet<string>
            {
                "a", "an", "and", "are", "as", "at", "be", "by", "for", "from",
                "has", "he", "in", "is", "it", "its", "of", "on", "that", "the",
                "to", "was", "will", "with", "what", "when", "where", "who", "how",
                "i", "me", "my", "you", "your", "can", "could", "should", "would",
                "do", "does", "did", "have", "had", "about", "this", "these", "those"
            };

            var words = text.ToLower()
                .Split(new[] { ' ', '.', ',', '!', '?', ';', ':', '\n', '\r' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 3 && !stopwords.Contains(w))
                .ToHashSet();

            return words;
        }

        /// <summary>
        /// Accurate embedding-based similarity using OpenAI.
        /// Calculates cosine similarity between message embeddings.
        /// </summary>
        private async Task<double> CalculateEmbeddingSimilarityAsync(
            string current,
            List<ConversationMessage> recent)
        {
            // Get embedding for current message
            var currentEmbedding = await GetEmbeddingAsync(current);

            // Get embeddings for recent messages and average them
            var recentEmbeddings = new List<double[]>();
            foreach (var msg in recent)
            {
                var embedding = await GetEmbeddingAsync(msg.Content);
                recentEmbeddings.Add(embedding);
            }

            // Average recent embeddings
            var avgRecentEmbedding = AverageEmbeddings(recentEmbeddings);

            // Calculate cosine similarity
            var similarity = CosineSimilarity(currentEmbedding, avgRecentEmbedding);

            return similarity;
        }

        /// <summary>
        /// Gets embedding vector from OpenAI API.
        /// Uses text-embedding-3-small (cheap and fast).
        /// </summary>
        private async Task<double[]> GetEmbeddingAsync(string text)
        {
            var requestBody = new
            {
                model = "text-embedding-3-small",
                input = text
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync(
                "https://api.openai.com/v1/embeddings",
                content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI API error: {response.StatusCode} - {error}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseBody);

            var embeddingArray = jsonResponse.RootElement
                .GetProperty("data")[0]
                .GetProperty("embedding");

            var embedding = new List<double>();
            foreach (var element in embeddingArray.EnumerateArray())
            {
                embedding.Add(element.GetDouble());
            }

            return embedding.ToArray();
        }

        /// <summary>
        /// Averages multiple embedding vectors.
        /// </summary>
        private double[] AverageEmbeddings(List<double[]> embeddings)
        {
            if (embeddings.Count == 0)
                throw new ArgumentException("No embeddings to average");

            var dimension = embeddings[0].Length;
            var avg = new double[dimension];

            for (int i = 0; i < dimension; i++)
            {
                double sum = 0;
                foreach (var embedding in embeddings)
                {
                    sum += embedding[i];
                }
                avg[i] = sum / embeddings.Count;
            }

            return avg;
        }

        /// <summary>
        /// Calculates cosine similarity between two vectors.
        /// Returns value from -1 to 1 (we normalize to 0-1).
        /// </summary>
        private double CosineSimilarity(double[] a, double[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have same length");

            double dotProduct = 0;
            double normA = 0;
            double normB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            normA = Math.Sqrt(normA);
            normB = Math.Sqrt(normB);

            if (normA == 0 || normB == 0)
                return 0;

            var similarity = dotProduct / (normA * normB);

            // Normalize from [-1, 1] to [0, 1]
            return (similarity + 1) / 2;
        }
    }

    /// <summary>
    /// Result of topic continuity detection.
    /// </summary>
    public class TopicContinuityResult
    {
        /// <summary>
        /// Similarity score from 0.0 (different topic) to 1.0 (same topic)
        /// </summary>
        public double SimilarityScore { get; set; }

        /// <summary>
        /// True if this message represents a topic change
        /// </summary>
        public bool IsTopicChange { get; set; }

        /// <summary>
        /// How was this detected? "keyword", "embedding", "keyword-fallback", "first-message"
        /// </summary>
        public string DetectionMethod { get; set; } = "";

        /// <summary>
        /// Human-readable reason for the decision
        /// </summary>
        public string Reason { get; set; } = "";
    }
}
