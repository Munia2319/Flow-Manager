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
    /// Image Department Node - Visual intelligence and image analysis using AI vision
    /// </summary>
    /// <remarks>
    /// <para>
    /// ImageDepartmentNode provides visual analysis capabilities using OpenAI's vision models (gpt-4o-mini).
    /// It can interpret images, answer questions about visual content, and provide grounded analysis of real-world scenes.
    /// </para>
    /// <para>
    /// <b>Capabilities:</b>
    /// <list type="bullet">
    ///   <item><description><b>Visual Q&amp;A:</b> Answer questions about image content</description></item>
    ///   <item><description><b>Object Detection:</b> Identify objects, text, signs, labels in images</description></item>
    ///   <item><description><b>Scene Understanding:</b> Describe environments, safety conditions, compliance issues</description></item>
    ///   <item><description><b>Damage Assessment:</b> Analyze damage to equipment, products, facilities</description></item>
    ///   <item><description><b>Quality Control:</b> Visual inspection for manufacturing/logistics</description></item>
    ///   <item><description><b>Context-Aware:</b> Handles follow-up questions about images using conversation history</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Configuration:</b>
    /// <list type="bullet">
    ///   <item><description><b>apiKey:</b> Required. OpenAI API key</description></item>
    ///   <item><description><b>imageUrl:</b> Optional. Can be provided in config or inputs</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Inputs:</b>
    /// <list type="bullet">
    ///   <item><description><b>input:</b> The question to ask about the image</description></item>
    ///   <item><description><b>imageUrl:</b> URL of the image to analyze (if not in config)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Outputs:</b> { "output": "1-3 sentence answer about the image" }
    /// </para>
    /// <para>
    /// <b>Performance:</b> Uses gpt-4o-mini with max_tokens=300 for concise responses. Temperature=0.3 for accurate visual analysis.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// {
    ///   "id": "image_dept",
    ///   "type": "ImageDepartment",
    ///   "config": {
    ///     "apiKey": "sk-...",
    ///     "imageUrl": "https://example.com/warehouse.jpg"
    ///   }
    /// }
    /// </code>
    ///
    /// Usage:
    /// <code>
    /// // Question: "What safety hazards do you see in this warehouse?"
    /// // Answer: "I can see several safety concerns: stacked pallets without proper securing,
    /// //          a wet floor without warning signs, and a forklift operating near pedestrian traffic."
    /// </code>
    /// </example>
    public class ImageDepartmentNode : Node
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string OPENAI_API_URL = "https://api.openai.com/v1/chat/completions";
        
        public ImageDepartmentNode()
        {
            Type = "ImageDepartment";
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
                
                var imageUrl = Config.ContainsKey("imageUrl")
                    ? Config["imageUrl"]?.ToString()
                    : inputs.ContainsKey("imageUrl")
                        ? inputs["imageUrl"]?.ToString()
                        : null;
                
                if (string.IsNullOrEmpty(imageUrl))
                {
                    throw new Exception("Image URL is required for ImageDepartment");
                }
                
                var systemPrompt = @"## SYSTEM PROMPT: ImageAnalyser
You are the ImageAnalyser system — a visual intelligence module designed to interpret real-world images and provide useful, grounded, human-readable answers to visual queries.

### YOUR OBJECTIVES
1. **Interpret the current user message** as your primary instruction.
2. Analyze the image accordingly, and provide a clear and useful answer based on what is **visibly present**.
3. ✨ **USE CONVERSATION CONTEXT**: If the user is asking a follow-up question about a previous image or topic, reference that context

### RESPONSE FORMAT
- Return a **single short paragraph** (1–3 fluent, natural sentences).
- For follow-ups: Explicitly connect to previous context when relevant

Now analyze the image and answer the user's question:";

                // ✨ NEW: Build messages with conversation history
                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                // ✨ NEW: Include conversation history if available
                if (Context != null && Context.ConversationHistory != null && Context.ConversationHistory.Count > 0)
                {
                    Console.WriteLine($"[ImageDepartmentNode] 📚 Including {Context.ConversationHistory.Count} messages from conversation history");

                    // Add previous conversation messages (text only, images are expensive)
                    // Note: We only include text from history, not previous images to save tokens
                    foreach (var msg in Context.ConversationHistory)
                    {
                        messages.Add(new { role = msg.Role, content = msg.Content });
                    }
                }
                else
                {
                    Console.WriteLine($"[ImageDepartmentNode] 📭 No conversation history available");
                }

                // Add the current user question WITH the image
                messages.Add(new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = userQuestion },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = imageUrl }
                        }
                    }
                });
                
                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = messages,
                    temperature = 0.3,
                    max_tokens = 300
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