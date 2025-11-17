using System;
using System.Collections.Generic;

namespace FlowManager.Core.Models
{
    /// <summary>
    /// Represents a complete workflow (DAG) containing nodes, edges, and metadata
    /// </summary>
    /// <remarks>
    /// <para>
    /// A Flow is the top-level container for a workflow definition. It describes:
    /// <list type="bullet">
    ///   <item><description><b>What</b> to execute (nodes)</description></item>
    ///   <item><description><b>How</b> they connect (edges)</description></item>
    ///   <item><description><b>Who</b> owns it (tenant)</description></item>
    ///   <item><description><b>When</b> it was created/modified (timestamps)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Structure:</b>
    /// <br/>
    /// A flow is a Directed Acyclic Graph (DAG) where nodes are processing units and edges define data flow.
    /// The DAGExecutor analyzes this structure to determine execution order using topological sorting.
    /// </para>
    /// <para>
    /// <b>Multi-Tenancy:</b>
    /// <br/>
    /// The TenantId property enables isolation between different clients/organizations sharing the same FlowManager instance.
    /// </para>
    /// <para>
    /// <b>Persistence:</b>
    /// <br/>
    /// Flows are typically serialized to JSON for storage. See FlowStorageService for persistence operations.
    /// </para>
    /// </remarks>
    /// <example>
    /// Minimal flow example:
    /// <code>
    /// var flow = new Flow
    /// {
    ///     Id = "my-flow",
    ///     Name = "Simple Greeting Flow",
    ///     Description = "Greets the user",
    ///     TenantId = "default",
    ///     Nodes = new List&lt;NodeDefinition&gt;
    ///     {
    ///         new() { Id = "input1", Type = "Input", Config = new() { {"text", "{{userInput}}"} } },
    ///         new() { Id = "greeter", Type = "OpenAIChat", Config = new() { {"model", "gpt-4o-mini"} } }
    ///     },
    ///     Edges = new List&lt;Edge&gt;
    ///     {
    ///         new() { From = "input1", To = "greeter" }
    ///     }
    /// };
    /// </code>
    /// </example>
    public class Flow
    {
        /// <summary>
        /// Unique identifier for this flow (e.g., "spam-detector-flow", "customer-support-router")
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Human-readable name displayed in the UI (e.g., "Spam Detection + Department Router Flow")
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Description of the flow's purpose and functionality (shown in UI and documentation)
        /// </summary>
        public string? Description { get; set; } = "";

        /// <summary>
        /// Tenant identifier for multi-tenancy support. Defaults to "default" for single-tenant deployments.
        /// </summary>
        /// <remarks>
        /// Used to isolate flows between different clients/organizations sharing the same FlowManager instance.
        /// </remarks>
        public string TenantId { get; set; } = "default";

        /// <summary>
        /// List of all nodes (processing units) in this flow
        /// </summary>
        /// <remarks>
        /// Each node represents a distinct operation (AI call, data transformation, routing, etc.)
        /// </remarks>
        public List<NodeDefinition> Nodes { get; set; } = new();

        /// <summary>
        /// List of all edges (connections) that define data flow between nodes
        /// </summary>
        /// <remarks>
        /// Edges can be conditional (only followed if a condition is met) or unconditional (always followed).
        /// </remarks>
        public List<Edge> Edges { get; set; } = new();

        /// <summary>
        /// Timestamp when this flow was created (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when this flow was last updated (UTC). Used by UI to show modification time.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional timestamp for last modification. Can be null if never modified after creation.
        /// </summary>
        public DateTime? LastModified { get; set; }
    }

    /// <summary>
    /// Node Definition - Serializable representation of a node for JSON storage
    /// </summary>
    /// <remarks>
    /// <para>
    /// NodeDefinition is the storage/serialization format for nodes. It contains only the data needed
    /// to reconstruct a node instance. The FlowOrchestrator converts NodeDefinition → Node during execution.
    /// </para>
    /// <para>
    /// <b>Relationship:</b>
    /// <br/>
    /// NodeDefinition (storage) → Node (runtime execution)
    /// </para>
    /// </remarks>
    public class NodeDefinition
    {
        /// <summary>
        /// Unique identifier for this node within the flow (e.g., "input1", "orchestrator", "tech_dept")
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Type of node to instantiate (e.g., "Input", "OpenAIChat", "Orchestrator", "Formatter")
        /// </summary>
        /// <remarks>
        /// Must match a registered node type in FlowOrchestrator's node registry.
        /// </remarks>
        public string? Type { get; set; }

        /// <summary>
        /// Node configuration settings (varies by node type)
        /// </summary>
        /// <remarks>
        /// <para>
        /// Configuration examples by node type:
        /// <list type="bullet">
        ///   <item><description><b>Input:</b> { "text": "{{userInput}}" }</description></item>
        ///   <item><description><b>OpenAIChat:</b> { "apiKey": "sk-...", "model": "gpt-4o-mini", "temperature": 0.7, "systemPrompt": "..." }</description></item>
        ///   <item><description><b>Orchestrator:</b> { "apiKey": "sk-...", "model": "gpt-4o-mini", "systemPrompt": "..." }</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public Dictionary<string, object> Config { get; set; } = new();

        /// <summary>
        /// Optional condition that determines if this node should execute
        /// </summary>
        /// <remarks>
        /// <para>
        /// If null/empty, the node always executes (default behavior).
        /// If specified, the condition must evaluate to true for the node to run.
        /// </para>
        /// <para>
        /// Example: <c>"{{user.tier}} equals 'premium'"</c>
        /// </para>
        /// </remarks>
        public string? Condition { get; set; }
    }

   
}
