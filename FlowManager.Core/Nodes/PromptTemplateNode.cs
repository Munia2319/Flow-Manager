using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FlowManager.Core.Models;

namespace FlowManager.Core.Nodes
{
    /// <summary>
    /// Prompt Template Node - Creates dynamic prompts by substituting variables in templates
    /// </summary>
    /// <remarks>
    /// <para>
    /// PromptTemplateNode enables dynamic prompt construction using template variables.
    /// It replaces <c>{{variableName}}</c> patterns with actual values from inputs.
    /// </para>
    /// <para>
    /// <b>Use Cases:</b>
    /// <list type="bullet">
    ///   <item><description><b>Dynamic Prompts:</b> Build AI prompts with runtime values</description></item>
    ///   <item><description><b>Message Templates:</b> Create personalized messages</description></item>
    ///   <item><description><b>Query Construction:</b> Build search queries from user input</description></item>
    ///   <item><description><b>Content Generation:</b> Populate templates with data</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Configuration Parameters:</b>
    /// <list type="bullet">
    ///   <item><description><b>template:</b> Required. String containing {{variable}} placeholders</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Variable Syntax:</b>
    /// <br/>
    /// Variables are defined using double curly braces: <c>{{variableName}}</c>
    /// <br/>
    /// Variable names must match keys in the inputs dictionary.
    /// <br/>
    /// Unmatched variables remain as-is in the output.
    /// </para>
    /// <para>
    /// <b>Outputs:</b>
    /// <list type="bullet">
    ///   <item><description><b>output:</b> The template with variables replaced by their values</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Performance:</b> Synchronous operation using regex pattern matching. Executes instantly.
    /// </para>
    /// </remarks>
    /// <example>
    /// JSON definition:
    /// <code>
    /// {
    ///   "id": "greeting_template",
    ///   "type": "PromptTemplate",
    ///   "config": {
    ///     "template": "Hello {{name}}, your order {{orderId}} is ready!"
    ///   }
    /// }
    /// </code>
    ///
    /// Given inputs:
    /// <code>
    /// { "name": "Alice", "orderId": "12345" }
    /// </code>
    ///
    /// Output:
    /// <code>
    /// "Hello Alice, your order 12345 is ready!"
    /// </code>
    ///
    /// Complex example:
    /// <code>
    /// {
    ///   "id": "search_query",
    ///   "type": "PromptTemplate",
    ///   "config": {
    ///     "template": "Search for {{topic}} in {{category}} published after {{year}}"
    ///   }
    /// }
    /// // Input: { "topic": "AI", "category": "Research", "year": "2023" }
    /// // Output: "Search for AI in Research published after 2023"
    /// </code>
    /// </example>
    public class PromptTemplateNode : Node
    {
        /// <summary>
        /// Initializes a new instance of the PromptTemplateNode class
        /// </summary>
        public PromptTemplateNode()
        {
            Type = "PromptTemplate";
        }

        /// <summary>
        /// Executes the prompt template node by replacing variables with values from inputs
        /// </summary>
        /// <param name="inputs">Dictionary of variable names and their values</param>
        /// <returns>Dictionary with "output" key containing the template with substituted variables</returns>
        /// <remarks>
        /// <para>
        /// Uses regex to find all <c>{{variableName}}</c> patterns and replaces them with corresponding
        /// values from the inputs dictionary. Variables not found in inputs remain unchanged.
        /// </para>
        /// <para>
        /// <b>Regex Pattern:</b> <c>@"\{\{(\w+)\}\}"</c> matches word characters inside double braces.
        /// </para>
        /// </remarks>
        public override Task<Dictionary<string, object>> ExecuteAsync(
            Dictionary<string, object> inputs)
        {
            // STEP 1: Mark as running
            Status = NodeStatus.Running;
            StartTime = System.DateTime.UtcNow;
            
            try
            {
                // STEP 2: Get template from config
                // Config looks like: { "template": "Hello {{name}}" }
                var template = Config.ContainsKey("template")
                    ? Config["template"]?.ToString() ?? ""
                    : "";
                
                // STEP 3: Replace {{variableName}} with values from inputs
                // Uses Regex to find all {{...}} patterns
                var result = Regex.Replace(template, @"\{\{(\w+)\}\}", match =>
                {
                    // Extract the variable name
                    // Example: {{name}} → extracts "name"
                    var varName = match.Groups[1].Value;
                    
                    // Look for this variable in inputs
                    if (inputs.ContainsKey(varName))
                    {
                        // Found it! Replace with the actual value
                        return inputs[varName]?.ToString() ?? "";
                    }
                    
                    // Not found, keep the original {{variableName}}
                    return match.Value;
                });
                
                // STEP 4: Put result in outputs
                Outputs["output"] = result;
                
                // STEP 5: Mark as successful
                Status = NodeStatus.Success;
                EndTime = System.DateTime.UtcNow;
                
                // STEP 6: Return outputs
                return Task.FromResult(Outputs);
            }
            catch (System.Exception ex)
            {
                // If something goes wrong:
                Status = NodeStatus.Error;
                Error = ex.Message;
                EndTime = System.DateTime.UtcNow;
                throw;
            }
        }
    }
}