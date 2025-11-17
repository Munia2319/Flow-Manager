using System;
using FlowManager.Core.Models;

namespace FlowManager.Core.Engine
{
    /// <summary>
    /// Edge Evaluator - Determines if an edge should be followed based on runtime conditions
    /// </summary>
    /// <remarks>
    /// <para>
    /// The EdgeEvaluator is a critical component for **dynamic, output-based routing**. It enables
    /// flows to make intelligent branching decisions based on the outputs of previously executed nodes.
    /// </para>
    /// <para>
    /// <b>Key Capability: Conditional Branching</b>
    /// </para>
    /// <para>
    /// Without EdgeEvaluator: All edges are always followed (static routing)
    /// <br/>
    /// With EdgeEvaluator: Edges can be conditional (dynamic routing based on node outputs)
    /// </para>
    /// <para>
    /// <b>Use Cases:</b>
    /// <list type="bullet">
    ///   <item><description><b>Department Routing:</b> Route to tech/billing/sales based on orchestrator decision</description></item>
    ///   <item><description><b>Confidence Thresholds:</b> Take different paths based on confidence scores</description></item>
    ///   <item><description><b>Feature Flags:</b> Enable/disable branches based on configuration</description></item>
    ///   <item><description><b>A/B Testing:</b> Route to different implementations based on criteria</description></item>
    ///   <item><description><b>Error Handling:</b> Route to fallback nodes when errors occur</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// Example edge with condition:
    /// <code>
    /// {
    ///   "from": "orchestrator",
    ///   "to": "tech_dept",
    ///   "condition": "{{orchestrator.targets}} contains 'tech'"
    /// }
    ///
    /// // Only follows this edge if orchestrator output contains 'tech'
    /// </code>
    /// </example>
    public class EdgeEvaluator
    {
        private readonly ConditionEvaluator _conditionEvaluator;

        public EdgeEvaluator()
        {
            _conditionEvaluator = new ConditionEvaluator();
        }

        /// <summary>
        /// Determines if an edge should be followed based on its conditional expression
        /// </summary>
        /// <param name="edge">The edge to evaluate (contains From, To, and Condition properties)</param>
        /// <param name="context">The execution context containing outputs from all previously executed nodes</param>
        /// <returns>
        /// <c>true</c> if the edge should be followed (either no condition exists or condition evaluates to true);
        /// <c>false</c> if the condition exists and evaluates to false
        /// </returns>
        /// <remarks>
        /// <para>
        /// <b>Behavior:</b>
        /// <list type="bullet">
        ///   <item><description><b>No condition (null/empty):</b> Always returns true (backward compatible, always follow)</description></item>
        ///   <item><description><b>Condition exists:</b> Delegates to ConditionEvaluator to evaluate the expression</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Condition Syntax Examples:</b>
        /// <code>
        /// "{{orchestrator.targets}} contains 'tech'"        → Check if targets contains 'tech'
        /// "{{classifier.confidence}} > 80"                  → Check if confidence > 80
        /// "{{spam_check.isSpam}} is true"                  → Check if isSpam is true
        /// "{{node1.action}} equals 'DepartmentHandoff'"    → Check if action equals value
        /// </code>
        /// </para>
        /// <para>
        /// This method uses a clever trick: it creates a temporary NodeDefinition with the edge's condition
        /// and reuses the existing ConditionEvaluator logic. This avoids code duplication and ensures
        /// consistent condition evaluation across nodes and edges.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var evaluator = new EdgeEvaluator();
        ///
        /// // Edge with no condition - always follow
        /// var edge1 = new Edge { From = "a", To = "b", Condition = null };
        /// bool result1 = evaluator.ShouldFollowEdge(edge1, context); // true
        ///
        /// // Edge with condition - follow only if condition met
        /// var edge2 = new Edge {
        ///     From = "orchestrator",
        ///     To = "tech_dept",
        ///     Condition = "{{orchestrator.targets}} contains 'tech'"
        /// };
        /// bool result2 = evaluator.ShouldFollowEdge(edge2, context); // true or false
        /// </code>
        /// </example>
        public bool ShouldFollowEdge(Edge edge, Models.ExecutionContext context)
        {
            // CASE 1: No condition specified
            // → Always follow the edge (default behavior, backward compatible)
            if (string.IsNullOrEmpty(edge.Condition))
            {
                Console.WriteLine($"[EdgeEvaluator] Edge {edge.From} → {edge.To}: No condition, always follow");
                return true;
            }

            // CASE 2: Condition specified
            // → Evaluate the condition using ConditionEvaluator

            Console.WriteLine($"[EdgeEvaluator] Evaluating edge condition: {edge.From} → {edge.To}");
            Console.WriteLine($"[EdgeEvaluator]   Condition: '{edge.Condition}'");

            // Create a temporary NodeDefinition to reuse ConditionEvaluator logic
            // This is a clever trick to avoid duplicating condition evaluation code
            var tempNode = new NodeDefinition
            {
                Id = $"edge_{edge.From}_{edge.To}",
                Condition = edge.Condition
            };

            // Evaluate using existing ConditionEvaluator
            bool result = _conditionEvaluator.ShouldNodeRun(tempNode, context);

            Console.WriteLine($"[EdgeEvaluator]   Result: {result} → {(result ? "FOLLOW" : "SKIP")} this edge");

            return result;
        }

        /// <summary>
        /// Gets a human-readable description of an edge's condition for UI/debugging purposes
        /// </summary>
        /// <param name="edge">The edge whose condition to describe</param>
        /// <returns>
        /// A string description: "Always execute" if no condition exists, or the condition string itself
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is useful for UI displays, debugging, and logging to help users understand
        /// when an edge will be followed.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var edge1 = new Edge { Condition = null };
        /// var desc1 = evaluator.GetConditionDescription(edge1);
        /// // Returns: "Always execute"
        ///
        /// var edge2 = new Edge { Condition = "{{node.value}} > 50" };
        /// var desc2 = evaluator.GetConditionDescription(edge2);
        /// // Returns: "{{node.value}} > 50"
        /// </code>
        /// </example>
        public string GetConditionDescription(Edge edge)
        {
            if (string.IsNullOrEmpty(edge.Condition))
            {
                return "Always execute";
            }

            return edge.Condition;
        }
    }
}
