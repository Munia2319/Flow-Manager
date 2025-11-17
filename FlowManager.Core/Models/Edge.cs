namespace FlowManager.Core.Models
{
    /// <summary>
    /// Represents a directed connection between two nodes in a flow
    /// </summary>
    /// <remarks>
    /// <para>
    /// An Edge defines how data flows from one node (From) to another node (To). It specifies:
    /// <list type="bullet">
    ///   <item><description><b>Source:</b> Which node produces the data (From)</description></item>
    ///   <item><description><b>Target:</b> Which node consumes the data (To)</description></item>
    ///   <item><description><b>Output Mapping:</b> Which output key from the source node (FromOutput)</description></item>
    ///   <item><description><b>Input Mapping:</b> Which input key on the target node (ToInput)</description></item>
    ///   <item><description><b>Condition:</b> Optional conditional expression for dynamic routing</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Conditional Edges - The Key to Dynamic Routing:</b>
    /// <br/>
    /// Edges can have conditions that determine if they should be followed. This enables:
    /// <list type="bullet">
    ///   <item><description>Department routing based on orchestrator decisions</description></item>
    ///   <item><description>Confidence-based branching</description></item>
    ///   <item><description>Feature flags and A/B testing</description></item>
    ///   <item><description>Error handling and fallback paths</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Multiple Edges to Same Target:</b>
    /// <br/>
    /// If multiple edges point to the same node with the same ToInput, values are combined into a List.
    /// This enables fan-in patterns where one node receives inputs from multiple sources.
    /// </para>
    /// </remarks>
    /// <example>
    /// Simple unconditional edge:
    /// <code>
    /// var edge = new Edge
    /// {
    ///     From = "input1",
    ///     To = "llm1",
    ///     FromOutput = "output",  // Default
    ///     ToInput = "input"        // Default
    /// };
    /// </code>
    ///
    /// Conditional edge for department routing:
    /// <code>
    /// var edge = new Edge
    /// {
    ///     From = "orchestrator",
    ///     To = "tech_dept",
    ///     FromOutput = "query",
    ///     ToInput = "input",
    ///     Condition = "{{orchestrator.targets}} contains 'tech'"
    /// };
    /// // This edge only executes if orchestrator output contains 'tech'
    /// </code>
    ///
    /// Multiple outputs to different inputs:
    /// <code>
    /// var edge1 = new Edge { From = "spam_check", To = "formatter", FromOutput = "output", ToInput = "spam_analysis" };
    /// var edge2 = new Edge { From = "tech_dept", To = "formatter", FromOutput = "output", ToInput = "tech_response" };
    /// var edge3 = new Edge { From = "billing_dept", To = "formatter", FromOutput = "output", ToInput = "billing_response" };
    /// // Formatter receives three separate inputs: spam_analysis, tech_response, billing_response
    /// </code>
    /// </example>
    public class Edge
    {
        /// <summary>
        /// ID of the source node that produces data
        /// </summary>
        /// <example>"input1", "orchestrator", "tech_dept"</example>
        public string? From { get; set; }

        /// <summary>
        /// ID of the target node that receives data
        /// </summary>
        /// <example>"llm1", "formatter", "output_node"</example>
        public string? To { get; set; }

        /// <summary>
        /// Output key from the source node's Outputs dictionary
        /// </summary>
        /// <remarks>
        /// <para>
        /// Defaults to "output". Nodes can have multiple output keys:
        /// <list type="bullet">
        ///   <item><description><b>Standard:</b> "output"</description></item>
        ///   <item><description><b>Orchestrator:</b> "action", "targets", "query", "confidence"</description></item>
        ///   <item><description><b>Classifier:</b> "output", "metadata", "confidence"</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string FromOutput { get; set; } = "output";

        /// <summary>
        /// Input key for the target node's inputs dictionary
        /// </summary>
        /// <remarks>
        /// <para>
        /// Defaults to "input". Nodes can accept multiple named inputs:
        /// <list type="bullet">
        ///   <item><description><b>Standard:</b> "input"</description></item>
        ///   <item><description><b>Formatter:</b> "spam_analysis", "tech_response", "billing_response"</description></item>
        ///   <item><description><b>Combiner:</b> "source1", "source2", "source3"</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string ToInput { get; set; } = "input";

        /// <summary>
        /// Optional conditional expression that must evaluate to true for this edge to be followed
        /// </summary>
        /// <remarks>
        /// <para>
        /// If null or empty, the edge is always followed (default behavior).
        /// If specified, EdgeEvaluator evaluates the condition against the ExecutionContext.
        /// </para>
        /// <para>
        /// <b>Condition Syntax Examples:</b>
        /// <list type="bullet">
        ///   <item><description>"{{orch.targets}} contains 'tech'" - Check if targets contains 'tech'</description></item>
        ///   <item><description>"{{classifier.confidence}} > 80" - Check if confidence exceeds 80</description></item>
        ///   <item><description>"{{validator.isValid}} is true" - Check if validation passed</description></item>
        ///   <item><description>"{{action.type}} equals 'DepartmentHandoff'" - Check action type</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// See ConditionEvaluator for full list of supported operators.
        /// </para>
        /// </remarks>
        /// <example>
        /// "{{orchestrator.targets}} contains 'Search'"
        /// "{{confidence}} >= 70"
        /// "{{isSpam}} is true"
        /// </example>
        public string? Condition { get; set; }
    }
}