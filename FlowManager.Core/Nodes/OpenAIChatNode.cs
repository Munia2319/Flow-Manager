using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FlowManager.Core.Models;

namespace FlowManager.Core.Nodes
{
    /// <summary>
    /// OpenAI Chat Node - Calls OpenAI API for AI-generated responses
    /// </summary>
    /// <remarks>
    /// <para>
    /// OpenAIChatNode is the most powerful and versatile node type. It integrates with OpenAI's
    /// chat completion API to generate intelligent responses, classify text, analyze content,
    /// and perform various AI-driven tasks.
    /// </para>
    /// <para>
    /// <b>Use Cases:</b>
    /// <list type="bullet">
    ///   <item><description><b>Text Generation:</b> Generate responses, summaries, explanations</description></item>
    ///   <item><description><b>Classification:</b> Categorize text (spam detection, sentiment analysis)</description></item>
    ///   <item><description><b>Transformation:</b> Translate, rewrite, simplify content</description></item>
    ///   <item><description><b>Analysis:</b> Extract insights, identify patterns</description></item>
    ///   <item><description><b>Q&amp;A:</b> Answer questions based on context</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Configuration Parameters:</b>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Parameter</term>
    ///     <description>Description</description>
    ///   </listheader>
    ///   <item>
    ///     <term>apiKey</term>
    ///     <description><b>Required.</b> OpenAI API key (starts with "sk-")</description>
    ///   </item>
    ///   <item>
    ///     <term>model</term>
    ///     <description><b>Optional.</b> Model name (default: "gpt-4o-mini"). Options: "gpt-4", "gpt-4-turbo", "gpt-4o", "gpt-4o-mini", "gpt-3.5-turbo"</description>
    ///   </item>
    ///   <item>
    ///     <term>systemPrompt</term>
    ///     <description><b>Optional.</b> Instructions that define the AI's behavior and role</description>
    ///   </item>
    ///   <item>
    ///     <term>temperature</term>
    ///     <description><b>Optional.</b> Creativity level (default: 0.7). Range: 0.0 (deterministic) to 2.0 (very creative)</description>
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Outputs:</b>
    /// <list type="bullet">
    ///   <item><description><b>output:</b> The AI's response text</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Performance:</b> This node makes an external HTTP API call, which typically takes 1-5 seconds
    /// depending on response length and model. The static HttpClient is reused to prevent socket exhaustion.
    /// </para>
    /// </remarks>
    /// <example>
    /// JSON definition for text classification:
    /// <code>
    /// {
    ///   "id": "spam_classifier",
    ///   "type": "OpenAIChat",
    ///   "config": {
    ///     "apiKey": "sk-...",
    ///     "model": "gpt-4o-mini",
    ///     "temperature": 0.2,
    ///     "systemPrompt": "Classify as SPAM or NOT_SPAM. Return JSON: {\"classification\": \"SPAM\", \"confidence\": 95}"
    ///   }
    /// }
    /// </code>
    ///
    /// JSON definition for customer support:
    /// <code>
    /// {
    ///   "id": "support_agent",
    ///   "type": "OpenAIChat",
    ///   "config": {
    ///     "apiKey": "sk-...",
    ///     "model": "gpt-4",
    ///     "temperature": 0.7,
    ///     "systemPrompt": "You are a friendly customer support agent. Help users with their questions."
    ///   }
    /// }
    /// </code>
    /// </example>
    public class OpenAIChatNode : Node
    {
        // STATIC HTTP CLIENT - Reused across all OpenAI calls
        // Why static? Better performance, prevents socket exhaustion
        private static readonly HttpClient _httpClient = new HttpClient();
        
        // OPENAI API ENDPOINT
        private const string OPENAI_API_URL = "https://api.openai.com/v1/chat/completions";
        
        /// <summary>
        /// Initializes a new instance of the OpenAIChatNode class
        /// </summary>
        public OpenAIChatNode()
        {
            Type = "OpenAIChat";
        }

        /// <summary>
        /// Executes the OpenAI Chat node by calling the OpenAI API with configured parameters
        /// </summary>
        /// <param name="inputs">Input dictionary, expects "input" key with user message text</param>
        /// <returns>Dictionary with "output" key containing the AI's response</returns>
        /// <exception cref="Exception">Thrown when API key is missing or API call fails</exception>
        /// <remarks>
        /// <para>
        /// This method performs the following steps:
        /// <list type="number">
        ///   <item><description>Retrieves configuration (apiKey, model, systemPrompt, temperature)</description></item>
        ///   <item><description>Extracts user message from inputs["input"]</description></item>
        ///   <item><description>Builds messages array (system + user messages)</description></item>
        ///   <item><description>Makes POST request to OpenAI API</description></item>
        ///   <item><description>Parses JSON response and extracts AI's reply</description></item>
        ///   <item><description>Returns reply in Outputs["output"]</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>API Endpoint:</b> https://api.openai.com/v1/chat/completions
        /// </para>
        /// <para>
        /// <b>Error Handling:</b> On failure, sets Status=Error, records error message, and re-throws exception.
        /// </para>
        /// </remarks>
        public override async Task<Dictionary<string, object>> ExecuteAsync(
            Dictionary<string, object> inputs)
        {
            // STEP 1: Mark as running
            Status = NodeStatus.Running;
            StartTime = DateTime.UtcNow;
            
            try
            {
                // STEP 2: Get configuration from Config
                
                // API Key (required)
                var apiKey = Config.ContainsKey("apiKey") 
                    ? Config["apiKey"]?.ToString() 
                    : throw new Exception("apiKey is required in config");

                // Model (optional, defaults to gpt-4o-mini)
                var model = Config.ContainsKey("model") 
                    ? Config["model"]?.ToString() 
                    : "gpt-4o-mini";

                // System Prompt (optional)
                var systemPrompt = Config.ContainsKey("systemPrompt") 
                    ? Config["systemPrompt"]?.ToString() ?? ""
                    : "";

                // Temperature (optional, defaults to 0.7)
                double temperature = 0.7;
                if (Config.ContainsKey("temperature"))
                {
                    var tempValue = Config["temperature"];
                    if (tempValue is double d)
                        temperature = d;
                    else if (tempValue is int i)
                        temperature = i;
                    else if (double.TryParse(tempValue?.ToString(), out var parsed))
                        temperature = parsed;
                }
                
                // User Message (from inputs)
                var userMessage = inputs.ContainsKey("input") 
                    ? inputs["input"]?.ToString() ?? ""
                    : "";
                
                // STEP 3: Build messages array
                var messages = new List<object>();
                
                // Add system message if provided
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    messages.Add(new 
                    { 
                        role = "system", 
                        content = systemPrompt 
                    });
                }
                
                // Add user message if provided
                if (!string.IsNullOrEmpty(userMessage))
                {
                    messages.Add(new 
                    { 
                        role = "user", 
                        content = userMessage 
                    });
                }
                
                // STEP 4: Prepare API request body
                var requestBody = new
                {
                    model = model,
                    messages = messages,
                    temperature = temperature
                };
                
                // STEP 5: Set up HTTP headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                
                // STEP 6: Make API call to OpenAI
                var response = await _httpClient.PostAsJsonAsync(
                    OPENAI_API_URL,
                    requestBody
                );
                
                // Check if request was successful
                response.EnsureSuccessStatusCode();
                
                // STEP 7: Parse the response
                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(responseBody);
                
                // Extract the content from the response
                // Response structure: { choices: [{ message: { content: "..." } }] }
                var content = jsonDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                
                // STEP 8: Store the result
                Outputs["output"] = content ?? "";
                
                // STEP 9: Mark as successful
                Status = NodeStatus.Success;
                EndTime = DateTime.UtcNow;
                
                // STEP 10: Return outputs
                return Outputs;
            }
            catch (Exception ex)
            {
                // If anything goes wrong, mark as error
                Status = NodeStatus.Error;
                Error = ex.Message;
                EndTime = DateTime.UtcNow;
                throw;
            }
        }
    }
}