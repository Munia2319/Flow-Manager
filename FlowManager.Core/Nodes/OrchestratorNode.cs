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
    /// Orchestrator Node - Intelligent routing engine that decides which departments/targets to activate
    /// </summary>
    /// <remarks>
    /// <para>
    /// OrchestratorNode is the "brain" of dynamic routing flows. It analyzes user queries using AI
    /// and makes intelligent decisions about which downstream nodes (departments, handlers, processors)
    /// should be activated. This enables **conditional branching** and **multi-department routing**.
    /// </para>
    /// <para>
    /// <b>Key Capabilities:</b>
    /// <list type="bullet">
    ///   <item><description><b>Multi-Target Routing:</b> Can activate multiple departments simultaneously</description></item>
    ///   <item><description><b>Intent Detection:</b> Identifies user intent from natural language</description></item>
    ///   <item><description><b>Confidence Scoring:</b> Provides confidence level for routing decisions</description></item>
    ///   <item><description><b>Clarification Requests:</b> Can request more info when intent is unclear</description></item>
    ///   <item><description><b>Context Awareness:</b> Uses conversation history for follow-up questions</description></item>
    ///   <item><description><b>Topic Change Detection:</b> Detects when user switches topics</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Configuration Parameters:</b>
    /// <list type="bullet">
    ///   <item><description><b>apiKey:</b> Required. OpenAI API key</description></item>
    ///   <item><description><b>model:</b> Optional. Model name (default: gpt-4o-mini)</description></item>
    ///   <item><description><b>systemPrompt:</b> Optional. Custom routing logic (has comprehensive defaults)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Outputs:</b>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Output Key</term>
    ///     <description>Description</description>
    ///   </listheader>
    ///   <item>
    ///     <term>action</term>
    ///     <description>"DepartmentHandoff" or "None"</description>
    ///   </item>
    ///   <item>
    ///     <term>targets</term>
    ///     <description>Comma-separated list of departments (e.g., "tech,billing")</description>
    ///   </item>
    ///   <item>
    ///     <term>query</term>
    ///     <description>The original user question (passed through)</description>
    ///   </item>
    ///   <item>
    ///     <term>confidence</term>
    ///     <description>Routing confidence (0-100)</description>
    ///   </item>
    ///   <item>
    ///     <term>needs_clarification</term>
    ///     <description>Boolean indicating if clarification needed</description>
    ///   </item>
    ///   <item>
    ///     <term>clarification_question</term>
    ///     <description>Question to ask user (if needs_clarification=true)</description>
    ///   </item>
    ///   <item>
    ///     <term>reasoning</term>
    ///     <description>Explanation of routing decision</description>
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Usage with Conditional Edges:</b>
    /// <br/>
    /// OrchestratorNode outputs are designed to work with conditional edges:
    /// <code>
    /// // Edge will only follow if targets contains 'tech'
    /// {
    ///   "from": "orchestrator",
    ///   "to": "tech_dept",
    ///   "condition": "{{orchestrator.targets}} contains 'tech'"
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// JSON definition:
    /// <code>
    /// {
    ///   "id": "orchestrator",
    ///   "type": "Orchestrator",
    ///   "config": {
    ///     "apiKey": "sk-...",
    ///     "model": "gpt-4o-mini",
    ///     "systemPrompt": "Route to: tech, billing, sales, support. Return JSON with Action and Pipeline."
    ///   }
    /// }
    /// </code>
    ///
    /// Typical routing flow:
    /// <code>
    /// Input → Orchestrator → [Conditional Branches]
    ///                       ├→ Tech Dept (if targets contains 'tech')
    ///                       ├→ Billing Dept (if targets contains 'billing')
    ///                       ├→ Sales Dept (if targets contains 'sales')
    ///                       └→ Support Dept (if targets contains 'support')
    /// </code>
    /// </example>
    public class OrchestratorNode : Node
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string OPENAI_API_URL = "https://api.openai.com/v1/chat/completions";

        /// <summary>
        /// Initializes a new instance of the OrchestratorNode class
        /// </summary>
        public OrchestratorNode()
        {
            Type = "Orchestrator";
        }

        /// <summary>
        /// Executes the orchestrator node to analyze user query and determine routing targets
        /// </summary>
        /// <param name="inputs">
        /// Dictionary containing:
        /// <list type="bullet">
        ///   <item><description><b>input:</b> Required. The user's question/query to route</description></item>
        ///   <item><description><b>context:</b> Optional. Conversation history (last 3 messages)</description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// Dictionary with 7 output keys:
        /// <list type="bullet">
        ///   <item><description><b>action:</b> "DepartmentHandoff" or "None"</description></item>
        ///   <item><description><b>targets:</b> Comma-separated department list (e.g., "Search,Function")</description></item>
        ///   <item><description><b>query:</b> The user's original question (passed through)</description></item>
        ///   <item><description><b>confidence:</b> Routing confidence score (0-100)</description></item>
        ///   <item><description><b>needs_clarification:</b> Boolean, true if query is ambiguous</description></item>
        ///   <item><description><b>clarification_question:</b> Question to ask user (if needs_clarification=true)</description></item>
        ///   <item><description><b>reasoning:</b> Explanation of routing decision for debugging</description></item>
        /// </list>
        /// </returns>
        /// <exception cref="Exception">Thrown when API key is missing or API call fails</exception>
        /// <remarks>
        /// <para>
        /// <b>Execution Flow:</b>
        /// <list type="number">
        ///   <item><description>Retrieves configuration (apiKey, model, systemPrompt)</description></item>
        ///   <item><description>Builds messages array with system prompt and conversation history</description></item>
        ///   <item><description>Calls OpenAI API for routing decision</description></item>
        ///   <item><description>Parses JSON response to extract routing metadata</description></item>
        ///   <item><description>Populates 7 output keys for use by conditional edges</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Custom vs Default Prompts:</b>
        /// <br/>
        /// If config contains "systemPrompt", uses custom prompt.
        /// <br/>
        /// Otherwise, uses comprehensive default prompt with department routing logic.
        /// </para>
        /// <para>
        /// <b>Available Routing Targets (Default Prompt):</b>
        /// <br/>
        /// Search, Data, Function, Image departments
        /// </para>
        /// <para>
        /// <b>JSON Response Format Expected:</b>
        /// <code>
        /// {
        ///   "Action": "DepartmentHandoff",
        ///   "Pipeline": [
        ///     {"Target": "Search"},
        ///     {"Target": "Function"}
        ///   ],
        ///   "Confidence": 85,
        ///   "NeedsClarification": false,
        ///   "ClarificationQuestion": "",
        ///   "Reasoning": "User is asking about product info, requires search and extraction"
        /// }
        /// </code>
        /// </para>
        /// <para>
        /// <b>Usage with Conditional Edges:</b>
        /// <br/>
        /// The "targets" output is designed for use in edge conditions:
        /// <code>
        /// {
        ///   "from": "orchestrator",
        ///   "to": "search_dept",
        ///   "condition": "{{orchestrator.targets}} contains 'Search'"
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <example>
        /// Example outputs for different scenarios:
        /// <code>
        /// // Scenario 1: Route to Search department
        /// {
        ///   "action": "DepartmentHandoff",
        ///   "targets": "Search",
        ///   "query": "What is our return policy?",
        ///   "confidence": "90",
        ///   "needs_clarification": "false",
        ///   "clarification_question": "",
        ///   "reasoning": "Question about company policy requires knowledge base search"
        /// }
        ///
        /// // Scenario 2: Route to multiple departments
        /// {
        ///   "action": "DepartmentHandoff",
        ///   "targets": "Search,Function",
        ///   "query": "Find product ABC-123 and generate a report",
        ///   "confidence": "85",
        ///   "needs_clarification": "false",
        ///   "clarification_question": "",
        ///   "reasoning": "Requires search for product info and function for report generation"
        /// }
        ///
        /// // Scenario 3: Needs clarification
        /// {
        ///   "action": "None",
        ///   "targets": "",
        ///   "query": "What about the thing?",
        ///   "confidence": "20",
        ///   "needs_clarification": "true",
        ///   "clarification_question": "Could you please specify what you're asking about?",
        ///   "reasoning": "Query is too vague to route accurately"
        /// }
        /// </code>
        /// </example>
        public override async Task<Dictionary<string, object>> ExecuteAsync(
            Dictionary<string, object> inputs)
        {
            Status = NodeStatus.Running;
            StartTime = DateTime.UtcNow;
            
            try
            {
                // Get API key from config
                var apiKey = Config.ContainsKey("apiKey")
                    ? Config["apiKey"]?.ToString()
                    : throw new Exception("apiKey is required");

                // Get model from config (defaults to gpt-4o-mini)
                var model = Config.ContainsKey("model")
                    ? Config["model"]?.ToString()
                    : "gpt-4o-mini";
                
                // Get user question from inputs
                var userQuestion = inputs.ContainsKey("input") 
                    ? inputs["input"]?.ToString() ?? ""
                    : "";
                
                // Get context if provided (last 3 conversation messages)
                var context = inputs.ContainsKey("context")
                    ? inputs["context"]?.ToString() ?? ""
                    : "";

                // ✨ NEW: Check for custom system prompt in config
                // If provided, use custom prompt. Otherwise, use default.
                string systemPrompt;

                if (Config.ContainsKey("systemPrompt") && !string.IsNullOrWhiteSpace(Config["systemPrompt"]?.ToString()))
                {
                    // Use custom system prompt from config
                    systemPrompt = Config["systemPrompt"]?.ToString() ?? "";
                    Console.WriteLine($"[OrchestratorNode] 🎨 Using CUSTOM system prompt ({systemPrompt.Length} chars)");
                }
                else
                {
                    // Use default system prompt
                    Console.WriteLine($"[OrchestratorNode] 📋 Using DEFAULT system prompt");
                    systemPrompt = @"## SYSTEM PROMPT: Orchestrator
You are the central orchestrator of a modular AI system that supports workers in logistics, retail, manufacturing, and field operations.
Your job is to analyze the user question and decide which department(s) to trigger.

### CRITICAL: MULTI-DEPARTMENT ROUTING
**You can and SHOULD activate MULTIPLE departments simultaneously when the user's question involves multiple tasks.**
- If a question clearly contains MULTIPLE distinct intents (e.g., search + extraction), activate ALL relevant departments
- The system supports parallel execution - multiple departments can work at the same time
- DO NOT ask the user to choose between tasks - just route to ALL relevant departments
- Only ask for clarification if the request is genuinely unclear or nonsensical

### DEPARTMENTS YOU CAN TRIGGER
- **Search** — Information lookup from company knowledge base (policies, procedures, manuals, safety docs, HR policies) AND web search. Trigger for ANY informational question including: company policies, employee benefits, PTO, safety procedures, emergency protocols, equipment maintenance, general knowledge, factual questions. The Search department has hybrid intelligence: it searches internal documents FIRST, then falls back to web if needed.
- **Data** — Legacy department for security/safety lookups. Prefer routing to Search instead for most informational queries.
- **Function** — Extract product numbers/codes OR generate reports/documentation. Trigger when user asks to extract product information OR explicitly requests report/document generation.
- **Image** — Visual analysis of images. Trigger when the user asks about analyzing, describing, identifying, or understanding images, photos, or visual content.

### OUTPUT FORMAT
Return a valid JSON object:
{
  ""Action"": ""None"" or ""DepartmentHandoff"",
  ""Pipeline"": [ { ""Target"": ""Search|Data|Function|Image"" } ],
  ""Confidence"": 0-100,
  ""NeedsClarification"": true/false,
  ""ClarificationQuestion"": ""optional question to ask user"",
  ""Reasoning"": ""brief explanation of decision""
}

### CONFIDENCE GUIDELINES (for multi-department routing)
- **90-100**: All intents are crystal clear
- **70-89**: Clear intents, confident routing to multiple departments
- **50-69**: Reasonably clear, proceed with routing
- **40-49**: Moderate ambiguity but can still route
- **Below 40**: Very ambiguous, request clarification

### WHEN TO REQUEST CLARIFICATION
Set ""NeedsClarification"": true ONLY when:
1. Intent is completely unclear or nonsensical
2. Missing critical information that prevents ANY routing
3. Confidence below 40

**DO NOT request clarification just because there are multiple intents - route to multiple departments instead!**

### ROUTING PHILOSOPHY
1. **Multi-task queries**: Route to ALL relevant departments simultaneously
2. **When in doubt**: Route to Search with moderate confidence
3. **Clear tasks**: Route with high confidence (80+)

The Search department is intelligent and will:
- Search internal Knowledge Base first (company docs, policies, procedures)
- Fall back to web search if KB doesn't have the answer
- Combine both sources when appropriate

### EXAMPLES:

User: ""When was Tesla founded?""
→ { ""Action"": ""DepartmentHandoff"", ""Pipeline"": [{ ""Target"": ""Search"" }], ""Confidence"": 95, ""NeedsClarification"": false, ""Reasoning"": ""Single clear search intent for factual information"" }

User: ""What are forklift safety procedures?""
→ { ""Action"": ""DepartmentHandoff"", ""Pipeline"": [{ ""Target"": ""Search"" }], ""Confidence"": 98, ""NeedsClarification"": false, ""Reasoning"": ""Clear safety question - Search will check KB first for company procedures"" }

User: ""I need information about Tesla and also extract product code ABC-123""
→ { ""Action"": ""DepartmentHandoff"", ""Pipeline"": [{ ""Target"": ""Search"" }, { ""Target"": ""Function"" }], ""Confidence"": 90, ""NeedsClarification"": false, ""Reasoning"": ""Two clear intents: (1) Search for Tesla info, (2) Extract product code. Both departments will execute in parallel."" }

User: ""Find info about Tesla and extract codes""
→ { ""Action"": ""DepartmentHandoff"", ""Pipeline"": [{ ""Target"": ""Search"" }, { ""Target"": ""Function"" }], ""Confidence"": 85, ""NeedsClarification"": false, ""Reasoning"": ""Two tasks detected: search and extraction. Routing to both departments for parallel execution."" }

User: ""Extract product code PLT-9384-XYZ""
→ { ""Action"": ""DepartmentHandoff"", ""Pipeline"": [{ ""Target"": ""Function"" }], ""Confidence"": 95, ""NeedsClarification"": false, ""Reasoning"": ""Clear extraction task with product code provided"" }

User: ""Search for battery safety and generate a maintenance report""
→ { ""Action"": ""DepartmentHandoff"", ""Pipeline"": [{ ""Target"": ""Search"" }, { ""Target"": ""Function"" }], ""Confidence"": 88, ""NeedsClarification"": false, ""Reasoning"": ""Two tasks: (1) Search for safety info, (2) Generate report. Both will run in parallel."" }

User: ""Safety stuff""
→ { ""Action"": ""None"", ""Pipeline"": [], ""Confidence"": 30, ""NeedsClarification"": true, ""ClarificationQuestion"": ""What specific safety information do you need? (e.g., forklift safety, battery charging, emergency procedures)"", ""Reasoning"": ""Too vague, need specific safety topic"" }

User: ""Hello""
→ { ""Action"": ""None"", ""Pipeline"": [], ""Confidence"": 100, ""NeedsClarification"": false, ""Reasoning"": ""Greeting, no action needed"" }

Analyze this user question and return ONLY the JSON response.";
                }

                // ✨ NEW: Build messages with conversation history
                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                // ✨ Phase 4: Check for topic changes in conversation
                bool hasTopicChange = false;
                double topicSimilarity = 1.0;

                // ✨ NEW: Include conversation history if available
                // This gives the orchestrator full context to detect follow-up questions
                if (Context != null && Context.ConversationHistory != null && Context.ConversationHistory.Count > 0)
                {
                    Console.WriteLine($"[OrchestratorNode] 📚 Including {Context.ConversationHistory.Count} messages from conversation history");

                    // Check if most recent user message indicates a topic change
                    var lastUserMessage = Context.ConversationHistory
                        .Where(m => m.Role == "user")
                        .LastOrDefault();

                    if (lastUserMessage != null)
                    {
                        hasTopicChange = lastUserMessage.IsTopicChange;
                        topicSimilarity = lastUserMessage.GetTopicSimilarity();

                        if (hasTopicChange)
                        {
                            Console.WriteLine($"[OrchestratorNode] 🔄 Topic change detected in recent message (similarity: {topicSimilarity:P0})");
                        }
                    }

                    // Add all previous messages from conversation history
                    foreach (var msg in Context.ConversationHistory)
                    {
                        messages.Add(new { role = msg.Role, content = msg.Content });
                    }
                }
                else
                {
                    Console.WriteLine($"[OrchestratorNode] 📭 No conversation history available (first message in conversation)");
                }

                // ✨ Phase 4: Add topic context note if there's a topic change
                // This helps the orchestrator understand the user is switching topics
                if (hasTopicChange)
                {
                    var topicNote = $"[SYSTEM NOTE: The user appears to be switching to a NEW TOPIC (topic similarity: {topicSimilarity:P0}). " +
                                   "This question may not be related to the previous conversation. Route based on the current question alone.]";
                    messages.Add(new { role = "system", content = topicNote });
                    Console.WriteLine($"[OrchestratorNode] 💡 Added topic change context to orchestrator");
                }

                // Add the current user question
                // NOTE: The current question should NOT be in ConversationHistory yet
                // It will be added by FlowRunner AFTER the flow completes
                messages.Add(new { role = "user", content = userQuestion });
                
                // API request
                var requestBody = new
                {
                    model = model ?? "gpt-4o-mini",
                    messages = messages,
                    temperature = 0.3, // Low temperature for consistent decisions
                    response_format = new { type = "json_object" } // Force JSON output
                };
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                
                var response = await _httpClient.PostAsJsonAsync(OPENAI_API_URL, requestBody);
                response.EnsureSuccessStatusCode();
                
                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(responseBody);
                
                // Extract the decision JSON
                var decisionJson = jsonDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                
                // Store both the raw JSON and parsed decision
                Outputs["decision_json"] = decisionJson ?? "";

                // ✨ NEW: Pass through the user's original question to downstream nodes
                Outputs["query"] = userQuestion;

                // Parse the decision to make it easy for other nodes to use
                var decision = JsonDocument.Parse(decisionJson ?? "{}");
                var action = decision.RootElement.GetProperty("Action").GetString();
                Outputs["action"] = action ?? "None";

                // Extract department targets
                var targets = new List<string>();
                if (decision.RootElement.TryGetProperty("Pipeline", out var pipeline))
                {
                    foreach (var item in pipeline.EnumerateArray())
                    {
                        if (item.TryGetProperty("Target", out var target))
                        {
                            var targetName = target.GetString();
                            if (!string.IsNullOrEmpty(targetName))
                            {
                                targets.Add(targetName);
                            }
                        }
                    }
                }
                Outputs["targets"] = string.Join(",", targets);

                // ✨ Phase 5: Extract confidence and clarification info
                if (decision.RootElement.TryGetProperty("Confidence", out var confidenceElement))
                {
                    var confidence = confidenceElement.GetInt32();
                    Outputs["confidence"] = confidence;
                    Console.WriteLine($"[OrchestratorNode] 🎯 Routing confidence: {confidence}%");
                }
                else
                {
                    Outputs["confidence"] = 100; // Default to high confidence for backward compatibility
                }

                if (decision.RootElement.TryGetProperty("NeedsClarification", out var needsClarElement))
                {
                    var needsClarification = needsClarElement.GetBoolean();
                    Outputs["needs_clarification"] = needsClarification;

                    if (needsClarification)
                    {
                        Console.WriteLine($"[OrchestratorNode] ❓ Clarification needed");
                    }
                }
                else
                {
                    Outputs["needs_clarification"] = false;
                }

                if (decision.RootElement.TryGetProperty("ClarificationQuestion", out var clarQuestionElement))
                {
                    var clarificationQuestion = clarQuestionElement.GetString();
                    Outputs["clarification_question"] = clarificationQuestion ?? "";

                    if (!string.IsNullOrEmpty(clarificationQuestion))
                    {
                        Console.WriteLine($"[OrchestratorNode] 💬 Clarification: {clarificationQuestion}");
                    }
                }

                if (decision.RootElement.TryGetProperty("Reasoning", out var reasoningElement))
                {
                    var reasoning = reasoningElement.GetString();
                    Outputs["reasoning"] = reasoning ?? "";
                    Console.WriteLine($"[OrchestratorNode] 💡 Reasoning: {reasoning}");
                }

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