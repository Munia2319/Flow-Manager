using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowManager.Core.Models
{
    /// <summary>
    /// Base class for all node types in the FlowManager system
    /// </summary>
    /// <remarks>
    /// <para>
    /// Node is an abstract base class that defines the contract for all processing units in a flow.
    /// Concrete node types (InputNode, OpenAIChatNode, OrchestratorNode, etc.) inherit from this class
    /// and implement the ExecuteAsync method to perform their specific operations.
    /// </para>
    /// <para>
    /// <b>Node Lifecycle:</b>
    /// <list type="number">
    ///   <item><description><b>Creation:</b> FlowOrchestrator creates node instance from NodeDefinition</description></item>
    ///   <item><description><b>Configuration:</b> Config dictionary is populated from NodeDefinition</description></item>
    ///   <item><description><b>Execution:</b> ExecuteAsync is called with inputs from predecessor nodes</description></item>
    ///   <item><description><b>Output:</b> Outputs dictionary is populated with results</description></item>
    ///   <item><description><b>Completion:</b> Status set to Success or Error</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Available Node Types:</b>
    /// <list type="bullet">
    ///   <item><description><b>InputNode:</b> Entry point for user data</description></item>
    ///   <item><description><b>OpenAIChatNode:</b> Call OpenAI API for AI responses</description></item>
    ///   <item><description><b>OrchestratorNode:</b> Intelligent routing to multiple departments/targets</description></item>
    ///   <item><description><b>FormatterNode:</b> Combine multiple inputs into formatted output</description></item>
    ///   <item><description><b>TransformNode:</b> Data transformation operations</description></item>
    ///   <item><description><b>PromptTemplateNode:</b> Build prompts from templates</description></item>
    ///   <item><description><b>SearchDepartmentNode:</b> Search knowledge base and web</description></item>
    ///   <item><description><b>DataDepartmentNode:</b> Handle data queries</description></item>
    ///   <item><description><b>FunctionDepartmentNode:</b> Execute functions, extract codes, generate reports</description></item>
    ///   <item><description><b>ImageDepartmentNode:</b> Analyze images with AI vision</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// Creating a custom node type:
    /// <code>
    /// public class MyCustomNode : Node
    /// {
    ///     public MyCustomNode()
    ///     {
    ///         Type = "MyCustom";
    ///     }
    ///
    ///     public override async Task&lt;Dictionary&lt;string, object&gt;&gt; ExecuteAsync(Dictionary&lt;string, object&gt; inputs)
    ///     {
    ///         Status = NodeStatus.Running;
    ///         StartTime = DateTime.UtcNow;
    ///
    ///         try
    ///         {
    ///             // Your custom logic here
    ///             var result = await DoSomethingAsync();
    ///
    ///             Outputs["output"] = result;
    ///             Status = NodeStatus.Success;
    ///             EndTime = DateTime.UtcNow;
    ///             return Outputs;
    ///         }
    ///         catch (Exception ex)
    ///         {
    ///             Status = NodeStatus.Error;
    ///             Error = ex.Message;
    ///             EndTime = DateTime.UtcNow;
    ///             throw;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class Node
    {
        /// <summary>
        /// Unique identifier for this node within the flow (e.g., "input1", "orchestrator", "tech_dept")
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Type name of this node (e.g., "Input", "OpenAIChat", "Orchestrator")
        /// </summary>
        /// <remarks>
        /// Used by UI for display and by FlowOrchestrator for node instantiation.
        /// </remarks>
        public string? Type { get; set; }

        /// <summary>
        /// Configuration dictionary containing node-specific settings
        /// </summary>
        /// <remarks>
        /// <para>
        /// Contents vary by node type:
        /// <list type="bullet">
        ///   <item><description><b>InputNode:</b> { "text": "{{userInput}}" }</description></item>
        ///   <item><description><b>OpenAIChatNode:</b> { "model": "gpt-4", "temperature": 0.7, "systemPrompt": "..." }</description></item>
        ///   <item><description><b>OrchestratorNode:</b> { "model": "gpt-4o-mini", "systemPrompt": "..." }</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public Dictionary<string, object> Config { get; set; } = new();

        /// <summary>
        /// Output dictionary containing results produced by this node after execution
        /// </summary>
        /// <remarks>
        /// <para>
        /// Most nodes produce an "output" key, but some nodes produce multiple outputs:
        /// <list type="bullet">
        ///   <item><description><b>Standard:</b> { "output": "result text" }</description></item>
        ///   <item><description><b>Orchestrator:</b> { "action": "DepartmentHandoff", "targets": "tech,billing", "query": "..." }</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public Dictionary<string, object> Outputs { get; set; } = new();

        /// <summary>
        /// Current execution status of this node
        /// </summary>
        public NodeStatus Status { get; set; } = NodeStatus.Pending;

        /// <summary>
        /// Error message if the node failed during execution
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Timestamp when this node started executing (UTC)
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Timestamp when this node finished executing (UTC)
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Execution context providing access to conversation history, node outputs, and flow metadata
        /// </summary>
        /// <remarks>
        /// Set by FlowOrchestrator before calling ExecuteAsync(). Provides nodes with full execution context.
        /// </remarks>
        protected ExecutionContext? Context { get; set; }

        /// <summary>
        /// Executes this node's operation asynchronously
        /// </summary>
        /// <param name="inputs">Dictionary of input values from predecessor nodes</param>
        /// <returns>Dictionary of output values to be consumed by successor nodes</returns>
        /// <remarks>
        /// <para>
        /// This is the main method that concrete node types must implement. It should:
        /// <list type="number">
        ///   <item><description>Set Status to Running and record StartTime</description></item>
        ///   <item><description>Perform the node's operation (AI call, transformation, etc.)</description></item>
        ///   <item><description>Populate Outputs dictionary with results</description></item>
        ///   <item><description>Set Status to Success and record EndTime</description></item>
        ///   <item><description>On error: Set Status to Error, record Error message, set EndTime, and throw</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Async/Await:</b> This method is async to support non-blocking I/O operations like API calls.
        /// </para>
        /// </remarks>
        /// <exception cref="Exception">Thrown when node execution fails (API error, validation error, etc.)</exception>
        public abstract Task<Dictionary<string, object>> ExecuteAsync(
            Dictionary<string, object> inputs
        );
    }
    
    /// <summary>
    /// Execution status of a node in the flow
    /// </summary>
    public enum NodeStatus
    {
        /// <summary>
        /// Node is waiting to start (all dependencies not yet satisfied)
        /// </summary>
        Pending,

        /// <summary>
        /// Node is currently executing
        /// </summary>
        Running,

        /// <summary>
        /// Node completed successfully
        /// </summary>
        Success,

        /// <summary>
        /// Node failed with an error (see Error property for details)
        /// </summary>
        Error
    }
}