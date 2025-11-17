using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FlowManager.Core.Models;

namespace FlowManager.Core.Nodes
{
    /// <summary>
    /// Transform Node - Applies text transformations using regex, C# expressions, or built-in operations
    /// </summary>
    /// <remarks>
    /// <para>
    /// TransformNode provides text manipulation capabilities for data cleanup, formatting, and processing.
    /// It supports multiple transformation types without requiring external API calls.
    /// </para>
    /// <para>
    /// <b>Use Cases:</b>
    /// <list type="bullet">
    ///   <item><description><b>Text Cleanup:</b> Remove whitespace, normalize formatting</description></item>
    ///   <item><description><b>Case Conversion:</b> Convert to uppercase/lowercase</description></item>
    ///   <item><description><b>Regex Operations:</b> Find and replace patterns</description></item>
    ///   <item><description><b>String Manipulation:</b> Trim, reverse, extract substrings</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Configuration Parameters:</b>
    /// <list type="bullet">
    ///   <item><description><b>transformType:</b> Type of transformation (regex, csharp, trim, upper, lower)</description></item>
    ///   <item><description><b>pattern:</b> Regex pattern (required for transformType=regex)</description></item>
    ///   <item><description><b>replacement:</b> Replacement string (required for transformType=regex)</description></item>
    ///   <item><description><b>expression:</b> C# expression (required for transformType=csharp)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Supported Transform Types:</b>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Type</term>
    ///     <description>Operation</description>
    ///   </listheader>
    ///   <item><term>regex</term><description>Regex find and replace using pattern and replacement</description></item>
    ///   <item><term>csharp</term><description>C# string method (toupper, tolower, trim, reverse, etc.)</description></item>
    ///   <item><term>trim</term><description>Remove leading/trailing whitespace</description></item>
    ///   <item><term>upper</term><description>Convert to uppercase</description></item>
    ///   <item><term>lower</term><description>Convert to lowercase</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Performance:</b> All transformations are synchronous and execute instantly (no API calls).
    /// </para>
    /// </remarks>
    /// <example>
    /// Regex transformation:
    /// <code>
    /// {
    ///   "id": "cleanup",
    ///   "type": "Transform",
    ///   "config": {
    ///     "transformType": "regex",
    ///     "pattern": "\\s+",
    ///     "replacement": " "
    ///   }
    /// }
    /// // Replaces multiple spaces with single space
    /// </code>
    ///
    /// C# expression:
    /// <code>
    /// {
    ///   "id": "uppercase",
    ///   "type": "Transform",
    ///   "config": {
    ///     "transformType": "csharp",
    ///     "expression": "toupper"
    ///   }
    /// }
    /// </code>
    ///
    /// Simple transformation:
    /// <code>
    /// {
    ///   "id": "trim_text",
    ///   "type": "Transform",
    ///   "config": {
    ///     "transformType": "trim"
    ///   }
    /// }
    /// </code>
    /// </example>
    public class TransformNode : Node
    {
        /// <summary>
        /// Initializes a new instance of the TransformNode class
        /// </summary>
        public TransformNode()
        {
            Type = "Transform";
        }

        /// <summary>
        /// Executes the transform node by applying the configured transformation to input text
        /// </summary>
        /// <param name="inputs">Input dictionary with "input" key containing text to transform</param>
        /// <returns>Dictionary with "output" key containing transformed text</returns>
        /// <exception cref="Exception">Thrown when regex pattern is missing or transformation fails</exception>
        /// <remarks>
        /// This method applies transformations synchronously based on transformType configuration.
        /// Defaults to "trim" if transformType is not specified or is unrecognized.
        /// </remarks>
        public override Task<Dictionary<string, object>> ExecuteAsync(
            Dictionary<string, object> inputs)
        {
            // STEP 1: Mark as running
            Status = NodeStatus.Running;
            StartTime = DateTime.UtcNow;
            
            try
            {
                // STEP 2: Get input text
                var inputText = inputs.ContainsKey("input")
                    ? inputs["input"]?.ToString() ?? ""
                    : "";
                
                // STEP 3: Get transformation type from config
                // Options: "regex", "csharp", "trim", "upper", "lower"
                var transformType = Config.ContainsKey("transformType")
                    ? Config["transformType"]?.ToString()?.ToLower()
                    : "trim";
                
                string result = inputText;
                
                // STEP 4: Apply the transformation
                switch (transformType)
                {
                    case "regex":
                        // Regex find and replace
                        // Config needs: "pattern" and "replacement"
                        result = ApplyRegex(inputText);
                        break;
                        
                    case "csharp":
                        // Simple C# expressions
                        // Config needs: "expression" (like "ToUpper", "Trim")
                        result = ApplyCSharpExpression(inputText);
                        break;
                        
                    case "trim":
                        // Remove leading/trailing whitespace
                        result = inputText.Trim();
                        break;
                        
                    case "upper":
                        // Convert to uppercase
                        result = inputText.ToUpper();
                        break;
                        
                    case "lower":
                        // Convert to lowercase
                        result = inputText.ToLower();
                        break;
                        
                    default:
                        // If unknown type, just trim
                        result = inputText.Trim();
                        break;
                }
                
                // STEP 5: Store result
                Outputs["output"] = result;
                
                // STEP 6: Mark as successful
                Status = NodeStatus.Success;
                EndTime = DateTime.UtcNow;
                
                return Task.FromResult(Outputs);
            }
            catch (Exception ex)
            {
                // If something goes wrong
                Status = NodeStatus.Error;
                Error = ex.Message;
                EndTime = DateTime.UtcNow;
                throw;
            }
        }
        
        // HELPER: Apply regex transformation
        private string ApplyRegex(string input)
        {
            // Get pattern and replacement from config
            var pattern = Config.ContainsKey("pattern")
                ? Config["pattern"]?.ToString() ?? ""
                : "";
            
            var replacement = Config.ContainsKey("replacement")
                ? Config["replacement"]?.ToString() ?? ""
                : "";
            
            if (string.IsNullOrEmpty(pattern))
            {
                throw new Exception("Regex pattern is required");
            }
            
            // Apply the regex replacement
            return Regex.Replace(input, pattern, replacement);
        }
        
        // HELPER: Apply C# expression
        private string ApplyCSharpExpression(string input)
        {
            // Get expression from config
            var expression = Config.ContainsKey("expression")
                ? Config["expression"]?.ToString()?.ToLower()
                : "";
            
            // Support common string operations
            return expression switch
            {
                "toupper" => input.ToUpper(),
                "tolower" => input.ToLower(),
                "trim" => input.Trim(),
                "trimstart" => input.TrimStart(),
                "trimend" => input.TrimEnd(),
                "reverse" => new string(input.ToCharArray().Reverse().ToArray()),
                _ => input // If unknown, return as-is
            };
        }
    }
}