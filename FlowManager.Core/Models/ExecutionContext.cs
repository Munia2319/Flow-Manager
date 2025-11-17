using System;
using System.Collections.Generic;

namespace FlowManager.Core.Models
{
    /// <summary>
    /// Execution Context - Tracks runtime state and results of a flow execution
    /// </summary>
    /// <remarks>
    /// <para>
    /// ExecutionContext is the "detailed execution report" that captures everything that happens
    /// during a flow execution. It stores node outputs, execution order, timing, errors, and
    /// conversation context.
    /// </para>
    /// <para>
    /// <b>Key Features:</b>
    /// <list type="bullet">
    ///   <item><description><b>Output Storage:</b> Captures outputs from every node execution</description></item>
    ///   <item><description><b>Execution Tracking:</b> Records execution order and parallel batches</description></item>
    ///   <item><description><b>Timing Analysis:</b> Tracks start/end times for performance monitoring</description></item>
    ///   <item><description><b>Error Collection:</b> Aggregates all errors that occur during execution</description></item>
    ///   <item><description><b>Conversation Context:</b> Provides conversation history for context-aware nodes</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Lifecycle:</b>
    /// <list type="number">
    ///   <item><description>Created by FlowOrchestrator at start of execution</description></item>
    ///   <item><description>Status = Running, StartTime set</description></item>
    ///   <item><description>NodeOutputs populated as each node completes</description></item>
    ///   <item><description>ExecutionOrder tracks sequence of node execution</description></item>
    ///   <item><description>On completion: Status = Completed/Failed, EndTime set</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// Accessing node outputs:
    /// <code>
    /// var context = new ExecutionContext();
    ///
    /// // After orchestrator node executes
    /// var targets = context.NodeOutputs["orchestrator"]["targets"];
    /// var confidence = context.NodeOutputs["orchestrator"]["confidence"];
    ///
    /// // Check execution order
    /// // ["input1", "orchestrator", "search_dept", "formatter"]
    /// var order = context.ExecutionOrder;
    ///
    /// // Check parallel execution batches
    /// // [["input1"], ["orchestrator"], ["search", "data", "function"], ["formatter"]]
    /// var batches = context.ExecutionBatches;
    /// </code>
    /// </example>
    public class ExecutionContext
    {
        /// <summary>
        /// ID of the flow being executed
        /// </summary>
        /// <example>"spam-and-router-flow", "customer-support-flow"</example>
        public string? FlowId { get; set; }

        /// <summary>
        /// Unique identifier for this specific execution instance
        /// </summary>
        /// <remarks>
        /// Same flow can be executed multiple times. Each execution gets a unique ID.
        /// Auto-generated GUID on creation.
        /// </remarks>
        /// <example>"exec-a1b2c3d4-20250105-143000", "f47ac10b-58cc-4372-a567-0e02b2c3d479"</example>
        public string ExecutionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Timestamp when flow execution started (UTC)
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when flow execution finished (UTC). Null if still running.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Current execution status (Running, Completed, Failed, Cancelled)
        /// </summary>
        public ExecutionStatus Status { get; set; } = ExecutionStatus.Running;

        /// <summary>
        /// Dictionary storing outputs from all executed nodes
        /// </summary>
        /// <remarks>
        /// <para>
        /// Structure: <c>{ "nodeId": { "outputName": value } }</c>
        /// </para>
        /// <para>
        /// Example:
        /// <code>
        /// {
        ///   "input1": { "output": "Hello" },
        ///   "orchestrator": { "action": "DepartmentHandoff", "targets": "Search,Function", "query": "..." },
        ///   "search_dept": { "output": "Search results..." },
        ///   "formatter": { "output": "Final formatted response" }
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        public Dictionary<string, Dictionary<string, object>> NodeOutputs { get; set; } = new();

        /// <summary>
        /// List of node IDs in the order they were executed (sequential timeline)
        /// </summary>
        /// <example>["input1", "orchestrator", "search_dept", "data_dept", "formatter"]</example>
        public List<string> ExecutionOrder { get; set; } = new();

        /// <summary>
        /// List of execution batches showing which nodes ran in parallel
        /// </summary>
        /// <remarks>
        /// <para>
        /// Each inner list represents nodes that executed simultaneously.
        /// Useful for visualizing parallel execution and performance optimization.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// [
        ///   ["input1"],                              // Batch 1: Entry point
        ///   ["orchestrator"],                        // Batch 2: Routing decision
        ///   ["search_dept", "data_dept", "function_dept"],  // Batch 3: Parallel execution!
        ///   ["formatter"]                            // Batch 4: Final aggregation
        /// ]
        /// </code>
        /// </example>
        public List<List<string>> ExecutionBatches { get; set; } = new();

        /// <summary>
        /// List of error messages that occurred during execution
        /// </summary>
        /// <example>["Node llm1 failed: API key invalid", "Node formatter1 timed out"]</example>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Conversation history providing context for this flow execution
        /// </summary>
        /// <remarks>
        /// <para>
        /// Enables context-aware processing. Nodes like OrchestratorNode use this to understand
        /// follow-up questions and maintain conversation continuity.
        /// </para>
        /// <para>
        /// Populated by FlowOrchestrator from ConversationMemory before execution.
        /// </para>
        /// </remarks>
        public List<ConversationMessage> ConversationHistory { get; set; } = new();
    }

    /// <summary>
    /// Execution status enumeration
    /// </summary>
    public enum ExecutionStatus
    {
        /// <summary>
        /// Flow is currently executing (in progress)
        /// </summary>
        Running,

        /// <summary>
        /// Flow completed successfully (all nodes succeeded)
        /// </summary>
        Completed,

        /// <summary>
        /// Flow failed (one or more nodes encountered errors)
        /// </summary>
        Failed,

        /// <summary>
        /// Flow execution was cancelled by user
        /// </summary>
        Cancelled
    }
}