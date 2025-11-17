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
    /// Data Department Node - Security procedures and safety protocols specialist
    /// </summary>
    /// <remarks>
    /// <para>
    /// DataDepartmentNode provides expert information on security procedures, safety protocols,
    /// operational guidelines, and compliance requirements for logistics, retail, manufacturing, and field operations.
    /// </para>
    /// <para>
    /// <b>Use Cases:</b>
    /// <list type="bullet">
    ///   <item><description><b>Safety Procedures:</b> Forklift safety, warehouse protocols, PPE requirements</description></item>
    ///   <item><description><b>Security Guidelines:</b> Access control, facility security, incident response</description></item>
    ///   <item><description><b>Operational Protocols:</b> Standard operating procedures (SOPs)</description></item>
    ///   <item><description><b>Compliance:</b> OSHA guidelines, industry standards, regulatory requirements</description></item>
    ///   <item><description><b>Context-Aware:</b> Handles follow-up questions using conversation history</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Configuration:</b>
    /// <list type="bullet">
    ///   <item><description><b>apiKey:</b> Required. OpenAI API key</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Outputs:</b> { "output": "Step-by-step procedure or guideline explanation" }
    /// </para>
    /// <para>
    /// <b>Temperature:</b> 0.3 (low) for consistent, factual, compliance-oriented answers
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// {
    ///   "id": "data_dept",
    ///   "type": "DataDepartment",
    ///   "config": { "apiKey": "sk-..." }
    /// }
    /// </code>
    /// </example>
    public class DataDepartmentNode : Node
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string OPENAI_API_URL = "https://api.openai.com/v1/chat/completions";
        
        public DataDepartmentNode()
        {
            Type = "DataDepartment";
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
                
                // ✨ ENHANCED: Data Department System Prompt with conversation context awareness
                var systemPrompt = @"## SYSTEM: Security and Safety Procedures Specialist
You are a knowledgeable specialist in security procedures and safety protocols for logistics, retail, manufacturing, and field operations.

Your task is to provide **clear, accurate, and actionable information** about procedures, protocols, and safety guidelines.

### RULES:
- Focus on **safety, security, and operational procedures**
- Provide **step-by-step instructions** when appropriate
- Emphasize **compliance and best practices**
- If you don't have specific company procedures, provide **industry-standard guidelines**
- ✨ **USE CONVERSATION CONTEXT**: If the user is asking a follow-up question, reference previous topics explicitly
- ✨ **BE CONTEXTUAL**: If they asked about ""forklift safety"" then ""battery charging"", answer about ""forklift battery charging safety""

### OUTPUT FORMAT:
- For simple questions: 1-2 sentence answer
- For complex procedures: Structured step-by-step format
- For follow-ups: Explicitly reference previous context (e.g., ""For forklift battery charging..."")

### EXAMPLES:
User: ""What are the forklift safety procedures?""
Output: ""Key forklift safety procedures include: 1) Always wear a seatbelt, 2) Inspect the forklift before use (brakes, steering, horn), 3) Never exceed the load capacity, 4) Keep forks low while traveling, 5) Use horn at intersections, 6) Only trained and certified operators should use forklifts. Always follow OSHA guidelines and your company's specific safety protocols.""

User: ""What about battery charging?"" (after asking about forklifts)
Output: ""For forklift battery charging safety: 1) Charge batteries in well-ventilated areas away from forklifts and flammable materials...""

Now answer the following question about procedures or safety protocols:";

                // ✨ NEW: Build messages with conversation history
                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                // ✨ NEW: Include conversation history if available
                if (Context != null && Context.ConversationHistory != null && Context.ConversationHistory.Count > 0)
                {
                    Console.WriteLine($"[DataDepartmentNode] 📚 Including {Context.ConversationHistory.Count} messages from conversation history");

                    // Add previous conversation messages
                    foreach (var msg in Context.ConversationHistory)
                    {
                        messages.Add(new { role = msg.Role, content = msg.Content });
                    }
                }
                else
                {
                    Console.WriteLine($"[DataDepartmentNode] 📭 No conversation history available");
                }

                // Add the current user question
                messages.Add(new { role = "user", content = userQuestion });
                
                // API request
                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = messages,
                    temperature = 0.3  // Lower temperature for factual, consistent answers
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