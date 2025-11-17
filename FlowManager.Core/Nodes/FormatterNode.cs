using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FlowManager.Core.Models;

namespace FlowManager.Core.Nodes
{
    /// <summary>
    /// Formatter Node - Combines outputs from multiple nodes into a cohesive, well-formatted response
    /// </summary>
    /// <remarks>
    /// <para>
    /// FormatterNode is the "final assembly" node that takes outputs from multiple predecessor nodes
    /// (such as different departments, classifiers, analyzers) and uses AI to combine them into
    /// a single, well-organized, user-friendly response.
    /// </para>
    /// <para>
    /// <b>Use Cases:</b>
    /// <list type="bullet">
    ///   <item><description><b>Multi-Department Aggregation:</b> Combine responses from tech, billing, sales departments</description></item>
    ///   <item><description><b>Fan-In Patterns:</b> Merge results from parallel processing branches</description></item>
    ///   <item><description><b>Response Organization:</b> Structure disparate information into clear sections</description></item>
    ///   <item><description><b>Conflict Resolution:</b> Handle redundant or conflicting information intelligently</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Configuration Parameters:</b>
    /// <list type="bullet">
    ///   <item><description><b>apiKey:</b> Required. OpenAI API key for formatting AI</description></item>
    ///   <item><description><b>systemPrompt:</b> Optional. Custom formatting instructions (has sensible defaults)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Input Handling:</b>
    /// <br/>
    /// FormatterNode accepts multiple named inputs from different sources. It automatically:
    /// <list type="bullet">
    ///   <item><description>Labels inputs based on their keys (search, data, function, image)</description></item>
    ///   <item><description>Handles both single values and lists (when multiple edges map to same input)</description></item>
    ///   <item><description>Adds visual separators and section headers</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Outputs:</b>
    /// <list type="bullet">
    ///   <item><description><b>output:</b> The formatted, combined response</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Intelligence:</b>
    /// <br/>
    /// Uses OpenAI (gpt-4o-mini) to:
    /// <list type="bullet">
    ///   <item><description>Remove redundancy across department responses</description></item>
    ///   <item><description>Prioritize successful results over failures</description></item>
    ///   <item><description>Create clear section headers</description></item>
    ///   <item><description>Maintain coherent narrative flow</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// JSON definition:
    /// <code>
    /// {
    ///   "id": "final_formatter",
    ///   "type": "Formatter",
    ///   "config": {
    ///     "apiKey": "sk-..."
    ///   }
    /// }
    /// </code>
    ///
    /// Typical usage pattern:
    /// <code>
    /// // Multiple departments feed into formatter
    /// spam_classifier → final_formatter (input: spam_analysis)
    /// tech_dept → final_formatter (input: tech_response)
    /// billing_dept → final_formatter (input: billing_response)
    /// sales_dept → final_formatter (input: sales_response)
    ///
    /// // Formatter combines all inputs into one organized response
    /// </code>
    /// </example>
    public class FormatterNode : Node
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string OPENAI_API_URL = "https://api.openai.com/v1/chat/completions";
        
        /// <summary>
        /// Initializes a new instance of the FormatterNode class
        /// </summary>
        public FormatterNode()
        {
            Type = "Formatter";
        }

        /// <summary>
        /// Executes the formatter node by combining multiple inputs into a formatted response using AI
        /// </summary>
        /// <param name="inputs">Dictionary of named inputs from multiple predecessor nodes</param>
        /// <returns>Dictionary with "output" key containing the formatted, combined response</returns>
        /// <exception cref="Exception">Thrown when API key is missing or API call fails</exception>
        /// <remarks>
        /// <para>
        /// This method:
        /// <list type="number">
        ///   <item><description>Collects all inputs and labels them by type (Search, Data, Function, Image)</description></item>
        ///   <item><description>Handles both single values and lists (from multiple edges)</description></item>
        ///   <item><description>Combines inputs with visual separators (---)</description></item>
        ///   <item><description>Calls OpenAI API with formatting instructions</description></item>
        ///   <item><description>Returns the AI-formatted response</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Empty Inputs:</b> If no inputs provided, returns a default "no information" message.
        /// </para>
        /// <para>
        /// <b>Input Labels:</b>
        /// <br/>
        /// Automatically adds emojis and labels: 🔍 SEARCH RESULTS, 📊 DATA ANALYSIS, ⚙️ FUNCTION OUTPUT, 🖼️ IMAGE ANALYSIS
        /// </para>
        /// </remarks>
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
                
                // ✨ Enhanced: Collect and label department outputs more clearly
                var departmentOutputs = new List<string>();

                // Map input keys to department types for better labeling
                var departmentLabels = new Dictionary<string, string>
                {
                    { "search", "🔍 SEARCH RESULTS" },
                    { "data", "📊 DATA ANALYSIS" },
                    { "function", "⚙️ FUNCTION OUTPUT" },
                    { "image", "🖼️ IMAGE ANALYSIS" },
                    { "input", "⚙️ EXTRACTED/PROCESSED" }
                };

                foreach (var input in inputs)
                {
                    // Get appropriate label
                    var label = departmentLabels.ContainsKey(input.Key.ToLower())
                        ? departmentLabels[input.Key.ToLower()]
                        : $"📌 {input.Key.ToUpper()}";

                    // ✨ Handle both single values and lists (when multiple sources map to same input key)
                    if (input.Value is List<object> list)
                    {
                        // Multiple values for this input key - add each one with index
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(list[i]?.ToString()))
                            {
                                departmentOutputs.Add($"{label} ({i + 1}):\n{list[i]}");
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(input.Value?.ToString()))
                    {
                        // Single value
                        departmentOutputs.Add($"{label}:\n{input.Value}");
                    }
                }

                var combinedOutputs = string.Join("\n\n---\n\n", departmentOutputs);
                
                if (string.IsNullOrEmpty(combinedOutputs))
                {
                    Outputs["output"] = "I couldn't find any information to answer your question.";
                    Status = NodeStatus.Success;
                    EndTime = DateTime.UtcNow;
                    return Outputs;
                }
                
                var systemPrompt = @"## SYSTEM PROMPT: LLM Formatter
You are the LLM Formatter in a modular assistant system with multiple specialized departments.
Your job is to take the raw outputs from different departments and turn them into a clear, well-organized response.

### CRITICAL RULES FOR MULTI-DEPARTMENT RESPONSES
1. **Clearly separate outputs from different departments** - Don't mix them into one blob
2. **Use section headers** to show what came from where (e.g., ""Here's what I found:"" for search, ""Extracted data:"" for function outputs)
3. **Prioritize successful extractions/actions** - If a Function department successfully extracted something, highlight that result prominently
4. **Handle conflicting information smartly** - If Search says ""couldn't find X"" but Function successfully extracted X, focus on the successful extraction
5. **Be concise but complete** - Present all department results, but remove redundancy

### OUTPUT FORMAT
Structure your response like this:
- If there are SEARCH results: Present them first with context
- If there are EXTRACTED/PROCESSED results: Present them clearly with emphasis
- If there are multiple departments: Use clear separators between sections

### EXAMPLES

Input:
🔍 SEARCH RESULTS:
[Long text about Tesla... ""couldn't find product code ABC-123""]

⚙️ EXTRACTED/PROCESSED:
ABC-123

Output:
**About Tesla:**
[Tesla information summary]

**Extracted Product Code:** ABC-123

---

Input:
🔍 SEARCH RESULTS:
Forklift safety procedures include...

⚙️ FUNCTION OUTPUT:
Generated safety report: report-2025-001.pdf

Output:
**Safety Information:**
Forklift safety procedures include...

**Generated Report:** report-2025-001.pdf

Now format the following outputs into a clear, organized response:";

                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = combinedOutputs }
                };
                
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
                
                var formattedAnswer = jsonDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                
                Outputs["output"] = formattedAnswer ?? combinedOutputs;
                
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