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
    /// Function Department Node - Data extraction and report generation specialist
    /// </summary>
    /// <remarks>
    /// <para>
    /// FunctionDepartmentNode is a dual-purpose node that handles both precise data extraction
    /// (product codes, stock numbers, identifiers) and structured report generation.
    /// </para>
    /// <para>
    /// <b>Capabilities:</b>
    /// <list type="bullet">
    ///   <item><description><b>Data Extraction:</b> Extract product codes, stock numbers, SKUs, identifiers from text</description></item>
    ///   <item><description><b>Report Generation:</b> Create structured reports, summaries, documentation</description></item>
    ///   <item><description><b>Auto-Detection:</b> Automatically switches between extraction and generation modes based on query</description></item>
    ///   <item><description><b>Custom Prompts:</b> Supports custom system prompts for specialized extraction/generation</description></item>
    ///   <item><description><b>Context-Aware:</b> Uses conversation history for multi-step extraction or report building</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Configuration:</b>
    /// <list type="bullet">
    ///   <item><description><b>apiKey:</b> Required. OpenAI API key</description></item>
    ///   <item><description><b>systemPrompt:</b> Optional. Custom prompt overrides auto-detection</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Auto-Detection Logic:</b>
    /// <br/>
    /// If query contains "extract", "product", "code", or "stock" → Extraction mode (temp=0.1)
    /// <br/>
    /// Otherwise → Report generation mode (temp=0.5)
    /// </para>
    /// <para>
    /// <b>Outputs:</b>
    /// <list type="bullet">
    ///   <item><description><b>Extraction:</b> Just the extracted code (e.g., "ABC-123")</description></item>
    ///   <item><description><b>Generation:</b> Structured report with headings and sections</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// Extraction:
    /// <code>
    /// // Input: "Extract product code from: Item XYZ-789 is available"
    /// // Output: "XYZ-789"
    /// </code>
    ///
    /// Report generation:
    /// <code>
    /// // Input: "Generate safety report for warehouse incident on 2025-01-05"
    /// // Output: Structured report with date, incident details, findings, recommendations
    /// </code>
    /// </example>
    public class FunctionDepartmentNode : Node
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string OPENAI_API_URL = "https://api.openai.com/v1/chat/completions";
        
        public FunctionDepartmentNode()
        {
            Type = "FunctionDepartment";
        }
        
        public override async Task<Dictionary<string, object>> ExecuteAsync(
            Dictionary<string, object> inputs)
        {
            Status = NodeStatus.Running;
            StartTime = DateTime.UtcNow;
            
            try
            {
                var apiKey = Config.ContainsKey("apiKey") 
                    ? Config["apiKey"]?.ToString() 
                    : throw new Exception("apiKey is required");
                
                var userQuestion = inputs.ContainsKey("input") 
                    ? inputs["input"]?.ToString() ?? ""
                    : "";
                
                var context = inputs.ContainsKey("context")
                    ? inputs["context"]?.ToString() ?? ""
                    : "";
                
                // ✨ NEW: Check for custom system prompt in config
                string systemPrompt;
                bool isExtraction;

                if (Config.ContainsKey("systemPrompt") && !string.IsNullOrWhiteSpace(Config["systemPrompt"]?.ToString()))
                {
                    // Use custom system prompt from config
                    systemPrompt = Config["systemPrompt"]?.ToString() ?? "";
                    Console.WriteLine($"[FunctionDepartment] 🎨 Using CUSTOM system prompt ({systemPrompt.Length} chars)");

                    // Default to moderate temperature for custom prompts
                    isExtraction = false;
                }
                else
                {
                    // Use default adaptive prompt based on task type
                    Console.WriteLine($"[FunctionDepartment] 📋 Using DEFAULT system prompt");

                    // Detect if this is extraction or report generation
                    var questionLower = userQuestion.ToLower();
                    isExtraction = questionLower.Contains("extract") ||
                                      questionLower.Contains("product") ||
                                      questionLower.Contains("code") ||
                                      questionLower.Contains("stock");

                    if (isExtraction)
                {
                    systemPrompt = @"## SYSTEM PROMPT: Product Code Extractor
You are a product code extraction specialist.

Your job is to extract the most probable product number (also called stock code or vasco_sifra) from the input.

### Rules:
- Look for alphanumeric codes that could be product numbers
- Common formats: ABC-123, 12345-678, XYZ-789-2024, etc.
- Return ONLY the code itself, no quotes, no labels, no extra text
- If multiple codes exist, return the most likely product code
- If absolutely no code found, return: NOT_FOUND
- ✨ **USE CONVERSATION CONTEXT**: If previous messages contain relevant product information, use that context

### Examples:
Input: ""Item XYZ-789-2024 is in stock""
Output: XYZ-789-2024

Input: ""Product ABC-123 costs $50""
Output: ABC-123

Now extract the product code:";
                }
                else
                {
                    // Report generation
                    systemPrompt = @"## SYSTEM PROMPT: Report Generator
You are a report generation specialist.

Your job is to generate well-structured reports, summaries, or documentation based on the user's request.

### Output Format:
- Use clear headings and sections
- Be professional and concise
- Include relevant details
- Structure information logically
- ✨ **USE CONVERSATION CONTEXT**: Build on information from previous messages when generating reports

Now generate the requested report or documentation:";
                    }
                }

                // ✨ NEW: Build messages with conversation history
                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                // ✨ NEW: Include conversation history if available
                if (Context != null && Context.ConversationHistory != null && Context.ConversationHistory.Count > 0)
                {
                    Console.WriteLine($"[FunctionDepartmentNode] 📚 Including {Context.ConversationHistory.Count} messages from conversation history");

                    // Add previous conversation messages
                    foreach (var msg in Context.ConversationHistory)
                    {
                        messages.Add(new { role = msg.Role, content = msg.Content });
                    }
                }
                else
                {
                    Console.WriteLine($"[FunctionDepartmentNode] 📭 No conversation history available");
                }

                // Add the current user question
                messages.Add(new { role = "user", content = userQuestion });
                
                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = messages,
                    temperature = isExtraction ? 0.1 : 0.5 // Lower temp for extraction, higher for generation
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
                
                Outputs["output"] = answer ?? "";
                
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
    }
}