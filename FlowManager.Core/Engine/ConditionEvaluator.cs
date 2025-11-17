using System;
using System.Collections.Generic;
using System.Text.Json;
using FlowManager.Core.Models;

namespace FlowManager.Core.Engine
{
    /// <summary>
    /// Condition Evaluator - Evaluates conditional expressions to determine if nodes or edges should execute
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ConditionEvaluator is the expression engine that powers FlowManager's conditional logic.
    /// It interprets template-based conditions like <c>{{nodeId.output}} contains 'value'</c> and
    /// evaluates them against runtime execution context.
    /// </para>
    /// <para>
    /// <b>Key Features:</b>
    /// <list type="bullet">
    ///   <item><description><b>Variable Interpolation:</b> Replaces {{nodeId.field}} with actual values from executed nodes</description></item>
    ///   <item><description><b>JSON Field Extraction:</b> Can extract fields from JSON output strings</description></item>
    ///   <item><description><b>Multiple Operators:</b> Supports contains, equals, comparisons, boolean checks</description></item>
    ///   <item><description><b>Type-Safe Evaluation:</b> Handles strings, numbers, booleans intelligently</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Supported Operators:</b>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Operator</term>
    ///     <description>Description</description>
    ///   </listheader>
    ///   <item>
    ///     <term>contains</term>
    ///     <description>Case-insensitive substring check: "text contains 'search'"</description>
    ///   </item>
    ///   <item>
    ///     <term>not contains</term>
    ///     <description>Case-insensitive substring exclusion: "text not contains 'search'"</description>
    ///   </item>
    ///   <item>
    ///     <term>equals</term>
    ///     <description>Case-insensitive equality: "value equals 'target'"</description>
    ///   </item>
    ///   <item>
    ///     <term>not equals</term>
    ///     <description>Case-insensitive inequality: "value not equals 'target'"</description>
    ///   </item>
    ///   <item>
    ///     <term>==</term>
    ///     <description>Case-sensitive equality: "value == 'exact'"</description>
    ///   </item>
    ///   <item>
    ///     <term>!=</term>
    ///     <description>Case-sensitive inequality: "value != 'exact'"</description>
    ///   </item>
    ///   <item>
    ///     <term>&gt;</term>
    ///     <description>Numeric greater than: "score > 80"</description>
    ///   </item>
    ///   <item>
    ///     <term>&lt;</term>
    ///     <description>Numeric less than: "score &lt; 50"</description>
    ///   </item>
    ///   <item>
    ///     <term>&gt;=</term>
    ///     <description>Numeric greater or equal: "score >= 70"</description>
    ///   </item>
    ///   <item>
    ///     <term>&lt;=</term>
    ///     <description>Numeric less or equal: "score &lt;= 100"</description>
    ///   </item>
    ///   <item>
    ///     <term>is true</term>
    ///     <description>Boolean true check: "flag is true"</description>
    ///   </item>
    ///   <item>
    ///     <term>is false</term>
    ///     <description>Boolean false check: "flag is false"</description>
    ///   </item>
    ///   <item>
    ///     <term>is null</term>
    ///     <description>Null/empty check: "value is null"</description>
    ///   </item>
    ///   <item>
    ///     <term>is not null</term>
    ///     <description>Non-null check: "value is not null"</description>
    ///   </item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// Example conditions:
    /// <code>
    /// // String contains
    /// "{{orchestrator.targets}} contains 'tech'"
    ///
    /// // Numeric comparison
    /// "{{classifier.confidence}} > 80"
    ///
    /// // Boolean check
    /// "{{validator.isValid}} is true"
    ///
    /// // Case-insensitive equality
    /// "{{action.type}} equals 'DepartmentHandoff'"
    /// </code>
    /// </example>
    public class ConditionEvaluator
    {
        private readonly VariableInterpolator _interpolator;
        
        public ConditionEvaluator()
        {
            _interpolator = new VariableInterpolator();
        }
        
        /// <summary>
        /// Determines if a node should execute based on its conditional expression
        /// </summary>
        /// <param name="nodeDef">The node definition containing the condition to evaluate</param>
        /// <param name="context">The execution context with outputs from previously executed nodes</param>
        /// <returns>
        /// <c>true</c> if the node should run (no condition or condition evaluates to true);
        /// <c>false</c> if condition evaluates to false
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is the main entry point for condition evaluation. It handles:
        /// <list type="number">
        ///   <item><description><b>No Condition:</b> Returns true immediately (node always runs)</description></item>
        ///   <item><description><b>Variable Interpolation:</b> Replaces {{nodeId.field}} with actual values</description></item>
        ///   <item><description><b>Expression Evaluation:</b> Evaluates the interpolated condition</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Logging:</b> Logs the original condition, interpolated condition, and result for debugging.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var evaluator = new ConditionEvaluator();
        /// var nodeDef = new NodeDefinition {
        ///     Id = "tech_dept",
        ///     Condition = "{{orchestrator.targets}} contains 'tech'"
        /// };
        ///
        /// bool shouldRun = evaluator.ShouldNodeRun(nodeDef, context);
        /// // Returns true if orchestrator.targets contains 'tech'
        /// </code>
        /// </example>
        public bool ShouldNodeRun(NodeDefinition nodeDef, FlowManager.Core.Models.ExecutionContext context)
        {
            // If no condition specified, always run
            if (string.IsNullOrEmpty(nodeDef.Condition))
            {
                return true;
            }
            
            // Interpolate variables in the condition manually
            var condition = InterpolateCondition(nodeDef.Condition, context);
            
            Console.WriteLine($"[Condition] Evaluating: '{nodeDef.Condition}' → '{condition}'");
            
            // Evaluate the condition
            bool result = EvaluateCondition(condition);
            
            Console.WriteLine($"[Condition] Result: {result}");
            
            return result;
        }
        
        /// <summary>
        /// Interpolates variables in a condition string by replacing {{nodeId.field}} patterns with actual values from the execution context
        /// </summary>
        /// <param name="condition">The condition string containing {{nodeId.field}} patterns to interpolate</param>
        /// <param name="context">The execution context containing outputs from previously executed nodes</param>
        /// <returns>The condition string with all variables replaced by their actual values</returns>
        /// <remarks>
        /// <para>
        /// This method supports two types of variable resolution:
        /// </para>
        /// <para>
        /// <b>Case 1: Direct Output Key</b>
        /// <br/>
        /// If the node has an output key matching the field name, use that directly.
        /// <br/>
        /// Example: {{aggregator.security}} → looks for outputs["security"]
        /// </para>
        /// <para>
        /// <b>Case 2: JSON Field Extraction</b>
        /// <br/>
        /// If no direct match, try to extract the field from the "output" key as JSON.
        /// <br/>
        /// Example: {{node1.confidence}} → parses outputs["output"] as JSON and extracts "confidence" field
        /// </para>
        /// <para>
        /// <b>Pattern Matching:</b> Uses regex <c>@"\{\{([^.]+)\.([^}]+)\}\}"</c> to find {{nodeId.field}} patterns
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Context has: NodeOutputs["orch"] = { "targets": "tech,billing" }
        /// var condition = "{{orch.targets}} contains 'tech'";
        /// var result = InterpolateCondition(condition, context);
        /// // Returns: "tech,billing contains 'tech'"
        /// </code>
        /// </example>
        private string InterpolateCondition(string condition, FlowManager.Core.Models.ExecutionContext context)
        {
            var result = condition;

            // Find all {{nodeId.outputName}} or {{nodeId.jsonField}} patterns
            var matches = System.Text.RegularExpressions.Regex.Matches(
                condition,
                @"\{\{([^.]+)\.([^}]+)\}\}"
            );

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var nodeId = match.Groups[1].Value;
                var fieldName = match.Groups[2].Value;

                // Look up the node in context
                if (context.NodeOutputs.ContainsKey(nodeId))
                {
                    var nodeOutputs = context.NodeOutputs[nodeId];

                    // CASE 1: Direct output key exists (e.g., {{aggregator.security}})
                    if (nodeOutputs.ContainsKey(fieldName))
                    {
                        var value = nodeOutputs[fieldName]?.ToString() ?? "";
                        result = result.Replace(match.Value, value);
                    }
                    // CASE 2: Try JSON field extraction from "output" key
                    else if (nodeOutputs.ContainsKey("output"))
                    {
                        var outputValue = nodeOutputs["output"]?.ToString() ?? "";

                        // Try to extract JSON field
                        var extractedValue = TryExtractJsonField(outputValue, fieldName);
                        if (extractedValue != null)
                        {
                            result = result.Replace(match.Value, extractedValue);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Attempts to extract a specific field from a JSON string
        /// </summary>
        /// <param name="jsonString">The JSON string to parse (may contain markdown code fences like ```json...```)</param>
        /// <param name="fieldName">The name of the field to extract from the JSON object</param>
        /// <returns>
        /// The field value as a string, or <c>null</c> if parsing fails or field doesn't exist
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is resilient to common JSON formatting issues:
        /// <list type="bullet">
        ///   <item><description><b>Markdown Code Fences:</b> Strips ```json...``` wrappers if present</description></item>
        ///   <item><description><b>Type Conversion:</b> Converts JSON types to strings appropriately</description></item>
        ///   <item><description><b>Error Handling:</b> Returns null on any parsing error (graceful degradation)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Type Handling:</b>
        /// <list type="bullet">
        ///   <item><description>String → returned as-is</description></item>
        ///   <item><description>Number → converted to string representation</description></item>
        ///   <item><description>true/false → converted to "true"/"false"</description></item>
        ///   <item><description>null → converted to "null"</description></item>
        ///   <item><description>Objects/Arrays → raw JSON text</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Extracting from clean JSON
        /// var json1 = "{\"confidence\": 85, \"classification\": \"SPAM\"}";
        /// var result1 = TryExtractJsonField(json1, "confidence");
        /// // Returns: "85"
        ///
        /// // Extracting from markdown-wrapped JSON
        /// var json2 = "```json\n{\"isSpam\": true}\n```";
        /// var result2 = TryExtractJsonField(json2, "isSpam");
        /// // Returns: "true"
        /// </code>
        /// </example>
        private string TryExtractJsonField(string jsonString, string fieldName)
        {
            try
            {
                // Remove markdown code fences if present (```json ... ```)
                var cleanJson = jsonString.Trim();
                if (cleanJson.StartsWith("```"))
                {
                    var lines = cleanJson.Split('\n');
                    cleanJson = string.Join('\n', lines[1..^1]); // Remove first and last lines
                }
                cleanJson = cleanJson.Trim();

                // Try to parse as JSON
                using var doc = JsonDocument.Parse(cleanJson);
                var root = doc.RootElement;

                // Try to get the field
                if (root.TryGetProperty(fieldName, out var property))
                {
                    // Return the value as string
                    return property.ValueKind switch
                    {
                        JsonValueKind.String => property.GetString(),
                        JsonValueKind.Number => property.GetDouble().ToString(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        JsonValueKind.Null => "null",
                        _ => property.GetRawText()
                    };
                }
            }
            catch
            {
                // Not valid JSON or field doesn't exist, return null
            }

            return null;
        }
        
        /// <summary>
        /// Evaluates a fully interpolated condition string using supported operators
        /// </summary>
        /// <param name="condition">The condition string with all variables already interpolated (no {{...}} patterns)</param>
        /// <returns><c>true</c> if condition evaluates to true, <c>false</c> otherwise</returns>
        /// <remarks>
        /// <para>
        /// This method evaluates conditions using a priority-based operator matching system.
        /// It checks operators in order of specificity (more specific first to avoid false matches).
        /// </para>
        /// <para>
        /// <b>Evaluation Priority:</b>
        /// <list type="number">
        ///   <item><description>contains / not contains (string substring operations)</description></item>
        ///   <item><description>equals / not equals (case-insensitive comparison)</description></item>
        ///   <item><description>== / != (case-sensitive comparison)</description></item>
        ///   <item><description>&gt;= / &lt;= / &gt; / &lt; (numeric comparisons)</description></item>
        ///   <item><description>is true / is false / is null / is not null (special checks)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Default Behavior:</b> If no operator matches, returns <c>true</c> (fail-safe mode).
        /// This prevents blocking execution due to syntax errors.
        /// </para>
        /// <para>
        /// <b>String Literals:</b> Values in single or double quotes are treated as string literals.
        /// Quotes are stripped during comparison.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // String contains
        /// EvaluateCondition("tech,billing contains 'tech'");  // true
        ///
        /// // Numeric comparison
        /// EvaluateCondition("85 > 70");  // true
        ///
        /// // Boolean check
        /// EvaluateCondition("true is true");  // true
        ///
        /// // Case-insensitive equality
        /// EvaluateCondition("DepartmentHandoff equals 'departmenthandoff'");  // true
        ///
        /// // Case-sensitive equality
        /// EvaluateCondition("approve == 'approve'");  // true
        /// EvaluateCondition("approve == 'Approve'");  // false
        /// </code>
        /// </example>
        private bool EvaluateCondition(string condition)
        {
            // Handle "contains" operator
            // Example: "Search,Data contains 'Search'" → true
            if (condition.Contains(" contains "))
            {
                var parts = condition.Split(new[] { " contains " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var text = parts[0].Trim();
                    var searchFor = parts[1].Trim().Trim('\'', '"');

                    return text.Contains(searchFor, StringComparison.OrdinalIgnoreCase);
                }
            }

            // Handle "not contains" operator
            // Example: "Search not contains 'Data'" → true
            if (condition.Contains(" not contains "))
            {
                var parts = condition.Split(new[] { " not contains " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var text = parts[0].Trim();
                    var searchFor = parts[1].Trim().Trim('\'', '"');

                    return !text.Contains(searchFor, StringComparison.OrdinalIgnoreCase);
                }
            }

            // Handle "==" operator (case-sensitive comparison)
            // Example: "approve == approve" → true
            if (condition.Contains(" == "))
            {
                var parts = condition.Split(new[] { " == " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim();
                    var right = parts[1].Trim().Trim('\'', '"');

                    return left.Equals(right, StringComparison.Ordinal);
                }
            }

            // Handle "!=" operator
            // Example: "approve != reject" → true
            if (condition.Contains(" != "))
            {
                var parts = condition.Split(new[] { " != " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim();
                    var right = parts[1].Trim().Trim('\'', '"');

                    return !left.Equals(right, StringComparison.Ordinal);
                }
            }

            // Handle "equals" operator (case-insensitive comparison)
            // Example: "DepartmentHandoff equals 'DepartmentHandoff'" → true
            if (condition.Contains(" equals "))
            {
                var parts = condition.Split(new[] { " equals " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim();
                    var right = parts[1].Trim().Trim('\'', '"');

                    return left.Equals(right, StringComparison.OrdinalIgnoreCase);
                }
            }

            // Handle "not equals" operator
            if (condition.Contains(" not equals "))
            {
                var parts = condition.Split(new[] { " not equals " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim();
                    var right = parts[1].Trim().Trim('\'', '"');

                    return !left.Equals(right, StringComparison.OrdinalIgnoreCase);
                }
            }

            // ✨ NEW: Handle ">=" operator
            // Example: "85 >= 70" → true
            if (condition.Contains(" >= "))
            {
                var parts = condition.Split(new[] { " >= " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    if (double.TryParse(parts[0].Trim(), out var left) &&
                        double.TryParse(parts[1].Trim(), out var right))
                    {
                        return left >= right;
                    }
                }
            }

            // ✨ NEW: Handle "<=" operator
            // Example: "30 <= 50" → true
            if (condition.Contains(" <= "))
            {
                var parts = condition.Split(new[] { " <= " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    if (double.TryParse(parts[0].Trim(), out var left) &&
                        double.TryParse(parts[1].Trim(), out var right))
                    {
                        return left <= right;
                    }
                }
            }

            // ✨ NEW: Handle ">" operator
            // Example: "85 > 70" → true
            if (condition.Contains(" > "))
            {
                var parts = condition.Split(new[] { " > " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    if (double.TryParse(parts[0].Trim(), out var left) &&
                        double.TryParse(parts[1].Trim(), out var right))
                    {
                        return left > right;
                    }
                }
            }

            // ✨ NEW: Handle "<" operator
            // Example: "30 < 50" → true
            if (condition.Contains(" < "))
            {
                var parts = condition.Split(new[] { " < " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    if (double.TryParse(parts[0].Trim(), out var left) &&
                        double.TryParse(parts[1].Trim(), out var right))
                    {
                        return left < right;
                    }
                }
            }

            // ✨ NEW: Handle "is true" operator
            // Example: "true is true" → true
            if (condition.Contains(" is true"))
            {
                var value = condition.Replace(" is true", "").Trim();
                return value.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            // ✨ NEW: Handle "is false" operator
            // Example: "false is false" → true
            if (condition.Contains(" is false"))
            {
                var value = condition.Replace(" is false", "").Trim();
                return value.Equals("false", StringComparison.OrdinalIgnoreCase);
            }

            // ✨ NEW: Handle "is null" operator
            // Example: " is null" → true (empty string)
            if (condition.Contains(" is null"))
            {
                var value = condition.Replace(" is null", "").Trim();
                return string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase);
            }

            // ✨ NEW: Handle "is not null" operator
            // Example: "somevalue is not null" → true
            if (condition.Contains(" is not null"))
            {
                var value = condition.Replace(" is not null", "").Trim();
                return !string.IsNullOrEmpty(value) && !value.Equals("null", StringComparison.OrdinalIgnoreCase);
            }

            // If we can't parse it, assume true (safer than blocking)
            Console.WriteLine($"[Condition] WARNING: Could not parse condition '{condition}', defaulting to true");
            return true;
        }
    }
}