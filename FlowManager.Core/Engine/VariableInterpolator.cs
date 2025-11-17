using System.Collections.Generic;
using System.Text.RegularExpressions;
using FlowManager.Core.Models;
using ExecutionContext = FlowManager.Core.Models.ExecutionContext;

namespace FlowManager.Core.Engine
{
    /// <summary>
    /// Variable Interpolator - Replaces template variables with actual node output values
    /// </summary>
    /// <remarks>
    /// <para>
    /// VariableInterpolator enables dynamic data flow between nodes by resolving template
    /// variables in the format <c>{{nodeId.outputKey}}</c> to actual values from ExecutionContext.
    /// This is the "glue" that connects node outputs to downstream node inputs.
    /// </para>
    /// <para>
    /// <b>Template Syntax:</b>
    /// <br/>
    /// <c>{{nodeId.outputKey}}</c>
    /// <br/>
    /// - <b>nodeId:</b> The ID of the node that produced the output
    /// <br/>
    /// - <b>outputKey:</b> The specific output property to reference
    /// </para>
    /// <para>
    /// <b>Resolution Process:</b>
    /// <list type="number">
    ///   <item><description>Find all <c>{{...}}</c> patterns using regex</description></item>
    ///   <item><description>Extract nodeId and outputKey by splitting on dot</description></item>
    ///   <item><description>Look up value in ExecutionContext.NodeOutputs[nodeId][outputKey]</description></item>
    ///   <item><description>Replace template with value.ToString() if found, otherwise keep original</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Use Cases:</b>
    /// <list type="bullet">
    ///   <item><description><b>Node Configuration:</b> Pass previous node outputs to current node config</description></item>
    ///   <item><description><b>Prompts:</b> Insert user input or previous AI responses into prompts</description></item>
    ///   <item><description><b>Conditional Edges:</b> Reference outputs in edge conditions</description></item>
    ///   <item><description><b>Data Pipeline:</b> Chain transformations across multiple nodes</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Error Handling:</b>
    /// <br/>
    /// If a variable cannot be resolved (node or output doesn't exist), the original template
    /// is preserved unchanged. This prevents breaking flows when optional outputs are missing.
    /// </para>
    /// </remarks>
    /// <example>
    /// Basic variable interpolation:
    /// <code>
    /// var interpolator = new VariableInterpolator();
    /// var context = new ExecutionContext();
    ///
    /// // Simulate node output
    /// context.NodeOutputs["input1"] = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["output"] = "Hello, world!"
    /// };
    ///
    /// var template = "User said: {{input1.output}}";
    /// var result = interpolator.Interpolate(template, context);
    /// // Result: "User said: Hello, world!"
    /// </code>
    ///
    /// Configuration interpolation:
    /// <code>
    /// var config = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["prompt"] = "Respond to: {{input1.output}}",
    ///     ["model"] = "gpt-4",  // Non-string values preserved
    ///     ["temperature"] = 0.7
    /// };
    ///
    /// var interpolatedConfig = interpolator.InterpolateConfig(config, context);
    /// // interpolatedConfig["prompt"] = "Respond to: Hello, world!"
    /// // interpolatedConfig["model"] = "gpt-4"
    /// // interpolatedConfig["temperature"] = 0.7
    /// </code>
    ///
    /// Multiple variables:
    /// <code>
    /// context.NodeOutputs["orchestrator"] = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["action"] = "DepartmentHandoff",
    ///     ["targets"] = "Search,Data"
    /// };
    ///
    /// var template = "Action: {{orchestrator.action}}, Departments: {{orchestrator.targets}}";
    /// var result = interpolator.Interpolate(template, context);
    /// // Result: "Action: DepartmentHandoff, Departments: Search,Data"
    /// </code>
    /// </example>
    public class VariableInterpolator
    {
        /// <summary>
        /// Replaces all template variables in a string with actual values from execution context
        /// </summary>
        /// <param name="template">
        /// Template string containing variables in <c>{{nodeId.outputKey}}</c> format.
        /// Can contain zero, one, or multiple variables.
        /// </param>
        /// <param name="context">
        /// ExecutionContext containing node outputs to resolve variables against.
        /// NodeOutputs dictionary structure: <c>{ "nodeId": { "outputKey": value } }</c>
        /// </param>
        /// <returns>
        /// String with all resolvable variables replaced by their actual values.
        /// Unresolvable variables (missing node or output) remain unchanged.
        /// Returns original string if template is null or empty.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <b>Regex Pattern:</b> <c>@"\{\{([^}]+)\}\}"</c>
        /// <br/>
        /// Matches any text inside double curly braces. Captures content for processing.
        /// </para>
        /// <para>
        /// <b>Variable Format:</b>
        /// <br/>
        /// Valid: <c>{{input1.output}}</c>, <c>{{orchestrator.action}}</c>, <c>{{llm1.response}}</c>
        /// <br/>
        /// Invalid: <c>{{input1}}</c> (missing outputKey), <c>{{.output}}</c> (missing nodeId)
        /// </para>
        /// <para>
        /// <b>Resolution Logic:</b>
        /// <list type="number">
        ///   <item><description>Extract path from <c>{{...}}</c> and trim whitespace</description></item>
        ///   <item><description>Split path by dot: <c>"input1.output"</c> → <c>["input1", "output"]</c></description></item>
        ///   <item><description>Validate format: must have at least 2 parts</description></item>
        ///   <item><description>Lookup nodeId in context.NodeOutputs</description></item>
        ///   <item><description>Lookup outputKey in node's outputs</description></item>
        ///   <item><description>Convert value to string via ToString()</description></item>
        ///   <item><description>If any step fails, keep original <c>{{...}}</c> template</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Null Handling:</b>
        /// <br/>
        /// If value exists but is null, empty string is returned for that variable.
        /// </para>
        /// </remarks>
        /// <example>
        /// Single variable:
        /// <code>
        /// var context = new ExecutionContext();
        /// context.NodeOutputs["input1"] = new Dictionary&lt;string, object&gt; { ["output"] = "Hello" };
        ///
        /// var result = interpolator.Interpolate("User said: {{input1.output}}", context);
        /// // Result: "User said: Hello"
        /// </code>
        ///
        /// Multiple variables:
        /// <code>
        /// context.NodeOutputs["user"] = new Dictionary&lt;string, object&gt; { ["name"] = "Alice", ["age"] = 30 };
        ///
        /// var result = interpolator.Interpolate("Name: {{user.name}}, Age: {{user.age}}", context);
        /// // Result: "Name: Alice, Age: 30"
        /// </code>
        ///
        /// Unresolvable variable (preserved):
        /// <code>
        /// var result = interpolator.Interpolate("Missing: {{unknown.value}}", context);
        /// // Result: "Missing: {{unknown.value}}" (unchanged)
        /// </code>
        /// </example>
        public string Interpolate(string template, ExecutionContext context)
        {
            // If template is null or empty, return as-is
            if (string.IsNullOrEmpty(template))
                return template;
            
            // Find all {{...}} patterns and replace them
            return Regex.Replace(template, @"\{\{([^}]+)\}\}", match =>
            {
                // Extract what's inside {{ }}
                // Example: {{input1.output}} → extracts "input1.output"
                var path = match.Groups[1].Value.Trim();
                
                // Split by dot: "input1.output" → ["input1", "output"]
                var parts = path.Split('.');
                
                // Need at least 2 parts: nodeId and outputName
                if (parts.Length < 2)
                {
                    // Invalid format, keep original
                    return match.Value;
                }
                
                var nodeId = parts[0];
                var outputName = parts[1];
                
                // Look up the value in context
                if (context.NodeOutputs.ContainsKey(nodeId))
                {
                    var nodeOutputs = context.NodeOutputs[nodeId];
                    
                    if (nodeOutputs.ContainsKey(outputName))
                    {
                        // Found it! Replace with the actual value
                        return nodeOutputs[outputName]?.ToString() ?? "";
                    }
                }
                
                // Not found, keep the original {{...}}
                return match.Value;
            });
        }

        /// <summary>
        /// Interpolates all string values in a configuration dictionary
        /// </summary>
        /// <param name="config">
        /// Node configuration dictionary containing key-value pairs.
        /// Only string values are interpolated; non-string values (int, bool, etc.) are preserved.
        /// </param>
        /// <param name="context">
        /// ExecutionContext containing node outputs to resolve variables against.
        /// NodeOutputs dictionary structure: <c>{ "nodeId": { "outputKey": value } }</c>
        /// </param>
        /// <returns>
        /// New dictionary with same keys and interpolated string values.
        /// Non-string values are copied unchanged. Original config is not modified.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <b>Processing Logic:</b>
        /// <br/>
        /// Iterates through each key-value pair in config:
        /// <list type="bullet">
        ///   <item><description><b>String values:</b> Pass through Interpolate() method to replace variables</description></item>
        ///   <item><description><b>Non-string values:</b> Copy directly to result (int, double, bool, objects, etc.)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Use Cases:</b>
        /// <br/>
        /// Primary use is preparing node configurations before node execution. FlowOrchestrator
        /// calls this method to replace {{nodeId.output}} in configs with actual values from
        /// previously executed nodes.
        /// </para>
        /// <para>
        /// <b>Immutability:</b>
        /// <br/>
        /// Creates a new dictionary; does not modify the original config parameter.
        /// </para>
        /// </remarks>
        /// <example>
        /// Node configuration interpolation:
        /// <code>
        /// var context = new ExecutionContext();
        /// context.NodeOutputs["input1"] = new Dictionary&lt;string, object&gt; { ["output"] = "What is AI?" };
        /// context.NodeOutputs["orchestrator"] = new Dictionary&lt;string, object&gt; { ["query"] = "AI definition" };
        ///
        /// var config = new Dictionary&lt;string, object&gt;
        /// {
        ///     ["prompt"] = "User asked: {{input1.output}}. Route as: {{orchestrator.query}}",
        ///     ["model"] = "gpt-4",
        ///     ["temperature"] = 0.7,
        ///     ["max_tokens"] = 500
        /// };
        ///
        /// var result = interpolator.InterpolateConfig(config, context);
        /// // result["prompt"] = "User asked: What is AI?. Route as: AI definition"
        /// // result["model"] = "gpt-4" (unchanged)
        /// // result["temperature"] = 0.7 (unchanged)
        /// // result["max_tokens"] = 500 (unchanged)
        /// </code>
        ///
        /// Mixed types preserved:
        /// <code>
        /// var config = new Dictionary&lt;string, object&gt;
        /// {
        ///     ["text"] = "Hello {{user.name}}",  // String → interpolated
        ///     ["count"] = 42,                     // Int → preserved
        ///     ["enabled"] = true,                 // Bool → preserved
        ///     ["ratio"] = 3.14                    // Double → preserved
        /// };
        ///
        /// var result = interpolator.InterpolateConfig(config, context);
        /// // Only "text" is interpolated, others copied as-is
        /// </code>
        /// </example>
        public Dictionary<string, object> InterpolateConfig(
            Dictionary<string, object> config,
            ExecutionContext context)
        {
            var result = new Dictionary<string, object>();
            
            foreach (var kvp in config)
            {
                // If the value is a string, interpolate it
                if (kvp.Value is string strValue)
                {
                    result[kvp.Key] = Interpolate(strValue, context);
                }
                else
                {
                    // Not a string, keep as-is
                    result[kvp.Key] = kvp.Value;
                }
            }
            
            return result;
        }
    }
}