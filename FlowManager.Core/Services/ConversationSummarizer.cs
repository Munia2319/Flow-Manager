using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FlowManager.Core.Models;

namespace FlowManager.Core.Services
{
    /// <summary>
    /// Intelligent conversation summarization using OpenAI.
    /// Takes older messages and condenses them into concise summaries that preserve key context.
    /// </summary>
    public class ConversationSummarizer
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;

        public ConversationSummarizer(string apiKey, string model = "gpt-4o-mini")
        {
            _httpClient = new HttpClient();
            _apiKey = apiKey;
            _model = model;
        }

        /// <summary>
        /// Generates an intelligent summary of conversation messages.
        /// Preserves important context like topics, departments, and key facts.
        /// </summary>
        /// <param name="messages">Messages to summarize</param>
        /// <returns>Concise summary text</returns>
        public async Task<string> SummarizeAsync(List<ConversationMessage> messages)
        {
            if (messages == null || messages.Count == 0)
            {
                return "SUMMARY: No previous messages.";
            }

            try
            {
                Console.WriteLine($"[ConversationSummarizer] 🤖 Generating summary for {messages.Count} messages...");

                // Build the conversation text for summarization
                var conversationText = new StringBuilder();
                foreach (var msg in messages)
                {
                    var dept = msg.Metadata.ContainsKey("triggeredDepartment")
                        ? $" [{msg.Metadata["triggeredDepartment"]}]"
                        : "";

                    conversationText.AppendLine($"{msg.Role.ToUpper()}{dept}: {msg.Content}");
                    conversationText.AppendLine();
                }

                // Create the summarization prompt
                var systemPrompt = @"You are a conversation summarizer for a multi-department AI assistant system.

Your job is to create CONCISE summaries of conversation history that preserve:
1. **Topics discussed** - What did the user ask about?
2. **Departments used** - Which departments handled requests (Search, Data, Function, Image)
3. **Key facts/answers** - Important information provided
4. **Context for follow-ups** - Information needed to understand recent messages

Keep summaries SHORT (1-3 sentences) but informative. Use this format:
SUMMARY: User asked about [topic] (handled by [Department]). Key points: [brief facts]. Then asked about [next topic] ([Department]). [Key facts].

Example:
SUMMARY: User asked about forklift safety procedures (Data dept). Key points: always wear seatbelt, check brakes before use, follow speed limits. Then asked about battery charging safety (Data dept). Must charge in ventilated areas, check water levels, avoid sparks.";

                var userPrompt = $@"Summarize this conversation:

{conversationText}

Provide a concise SUMMARY following the format above.";

                // Build OpenAI API request
                var requestBody = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    temperature = 0.3,  // Lower temperature for more focused summaries
                    max_tokens = 300    // Keep summaries concise
                };

                var jsonRequest = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[ConversationSummarizer] ❌ OpenAI API error: {response.StatusCode}");
                    Console.WriteLine($"[ConversationSummarizer] Response: {responseBody}");
                    return GenerateFallbackSummary(messages);
                }

                var jsonResponse = JsonDocument.Parse(responseBody);
                var summary = jsonResponse.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                Console.WriteLine($"[ConversationSummarizer] ✅ Summary generated: {summary?.Substring(0, Math.Min(100, summary?.Length ?? 0))}...");

                return summary ?? GenerateFallbackSummary(messages);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConversationSummarizer] ❌ Error during summarization: {ex.Message}");
                return GenerateFallbackSummary(messages);
            }
        }

        /// <summary>
        /// Generates a simple fallback summary when OpenAI is unavailable.
        /// Less intelligent but still preserves basic context.
        /// </summary>
        private string GenerateFallbackSummary(List<ConversationMessage> messages)
        {
            Console.WriteLine($"[ConversationSummarizer] ⚠️ Using fallback summary generation");

            var summary = new StringBuilder("SUMMARY: ");

            // Count user questions
            int userQuestions = 0;
            foreach (var msg in messages)
            {
                if (msg.Role == "user")
                    userQuestions++;
            }

            summary.Append($"Previous conversation with {userQuestions} user question(s). ");

            // Add first and last user messages
            ConversationMessage? firstUser = null;
            ConversationMessage? lastUser = null;

            foreach (var msg in messages)
            {
                if (msg.Role == "user")
                {
                    if (firstUser == null)
                        firstUser = msg;
                    lastUser = msg;
                }
            }

            if (firstUser != null)
            {
                var preview = firstUser.Content.Substring(0, Math.Min(50, firstUser.Content.Length));
                summary.Append($"Started with: \"{preview}...\" ");
            }

            if (lastUser != null && lastUser != firstUser)
            {
                var preview = lastUser.Content.Substring(0, Math.Min(50, lastUser.Content.Length));
                summary.Append($"Most recent: \"{preview}...\"");
            }

            return summary.ToString();
        }
    }
}
