using System.Collections.Generic;
using System.Threading.Tasks;
using FlowManager.Core.Models;

namespace FlowManager.Core.Nodes
{
    /// <summary>
    /// Input Node - Entry point for user data into a flow
    /// </summary>
    /// <remarks>
    /// <para>
    /// InputNode is the simplest node type and serves as the starting point for most flows.
    /// It takes text from its configuration and passes it to downstream nodes.
    /// </para>
    /// <para>
    /// <b>Use Cases:</b>
    /// <list type="bullet">
    ///   <item><description><b>User Input:</b> Capture text from user (via {{userInput}} template variable)</description></item>
    ///   <item><description><b>Static Text:</b> Provide hardcoded text for testing or configuration</description></item>
    ///   <item><description><b>Template Variables:</b> Use {{variableName}} syntax for dynamic values</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Configuration:</b>
    /// <code>
    /// {
    ///   "text": "{{userInput}}"  // Will be interpolated at runtime
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <b>Outputs:</b>
    /// <list type="bullet">
    ///   <item><description><b>output:</b> The text value from config (after interpolation)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// JSON definition:
    /// <code>
    /// {
    ///   "id": "input1",
    ///   "type": "Input",
    ///   "config": {
    ///     "text": "{{userInput}}"
    ///   }
    /// }
    /// </code>
    ///
    /// Usage in flow:
    /// <code>
    /// Input Node → captures user text → passes to next node
    /// </code>
    /// </example>
    public class InputNode : Node
    {
        /// <summary>
        /// Initializes a new instance of the InputNode class
        /// </summary>
        public InputNode()
        {
            Type = "Input";
        }

        /// <summary>
        /// Executes the input node by retrieving text from configuration and outputting it
        /// </summary>
        /// <param name="inputs">Input dictionary (typically unused for InputNode as it's usually a start node)</param>
        /// <returns>Dictionary with "output" key containing the text from configuration</returns>
        /// <remarks>
        /// <para>
        /// This method:
        /// <list type="number">
        ///   <item><description>Retrieves "text" value from Config dictionary</description></item>
        ///   <item><description>Stores it in Outputs["output"]</description></item>
        ///   <item><description>Returns immediately (synchronous operation wrapped in Task)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Performance:</b> This is a synchronous operation with negligible overhead.
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
                // STEP 2: Get text from config
                // Config looks like: { "text": "Hello World" }
                var text = Config.ContainsKey("text") 
                    ? Config["text"]?.ToString() ?? "" 
                    : "";
                
                // STEP 3: Put text in outputs
                Outputs["output"] = text;
                
                // STEP 4: Mark as successful
                Status = NodeStatus.Success;
                EndTime = System.DateTime.UtcNow;
                
                // STEP 5: Return outputs
                return Task.FromResult(Outputs);
            }
            catch (System.Exception ex)
            {
                // If something goes wrong:
                Status = NodeStatus.Error;
                Error = ex.Message;
                EndTime = System.DateTime.UtcNow;
                throw;  // Re-throw so orchestrator knows it failed
            }
        }
    }
}