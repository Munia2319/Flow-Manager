using System;
using System.Collections.Generic;
using System.Linq;
using FlowManager.Core.Models;

namespace FlowManager.Core.Engine
{
    /// <summary>
    /// DAG (Directed Acyclic Graph) Executor - Determines the execution order of nodes in a flow
    /// </summary>
    /// <remarks>
    /// <para>
    /// The DAGExecutor is the core scheduling engine that analyzes a flow's structure and determines
    /// the optimal execution order using Kahn's Algorithm (topological sorting). It ensures that
    /// nodes execute only after all their dependencies have completed.
    /// </para>
    /// <para>
    /// <b>Key Responsibilities:</b>
    /// <list type="bullet">
    ///   <item><description>Build graph representation from flow edges</description></item>
    ///   <item><description>Calculate topological sort for execution order</description></item>
    ///   <item><description>Detect circular dependencies (cycles)</description></item>
    ///   <item><description>Gather inputs for nodes from predecessor nodes</description></item>
    ///   <item><description>Identify start nodes (nodes with no dependencies)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Algorithm:</b> Uses Kahn's Algorithm for topological sorting:
    /// <list type="number">
    ///   <item><description>Count in-degree (incoming edges) for each node</description></item>
    ///   <item><description>Queue all nodes with in-degree = 0 (no dependencies)</description></item>
    ///   <item><description>Process queue: remove node, reduce neighbor in-degrees</description></item>
    ///   <item><description>Add newly freed nodes (in-degree = 0) to queue</description></item>
    ///   <item><description>If not all nodes processed → cycle detected!</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// Example usage:
    /// <code>
    /// var executor = new DAGExecutor();
    /// var flow = LoadFlow("my-flow.json");
    ///
    /// // Get execution order
    /// var order = executor.GetExecutionOrder(flow);
    /// // Output: ["input1", "orchestrator", "dept1", "dept2", "formatter"]
    ///
    /// // Find start nodes
    /// var startNodes = executor.GetStartNodes(flow);
    /// // Output: ["input1"]
    /// </code>
    /// </example>
    public class DAGExecutor
    {
        /// <summary>
        /// Builds an adjacency list graph representation from a flow's edges
        /// </summary>
        /// <param name="flow">The flow containing nodes and edges to convert into a graph</param>
        /// <returns>
        /// Dictionary where key = node ID, value = list of node IDs it points to
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method converts the flow's edge list into an adjacency list representation,
        /// which is a standard data structure for graph algorithms.
        /// </para>
        /// <para>
        /// <b>Graph Structure:</b>
        /// <code>
        /// {
        ///   "nodeA": ["nodeB", "nodeC"],  // nodeA points to B and C
        ///   "nodeB": ["nodeD"],           // nodeB points to D
        ///   "nodeC": [],                  // nodeC points to nothing
        ///   "nodeD": []                   // nodeD points to nothing
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var executor = new DAGExecutor();
        /// var graph = executor.BuildGraph(myFlow);
        /// // Returns adjacency list representation
        /// </code>
        /// </example>
        public Dictionary<string, List<string>> BuildGraph(Flow flow)
        {
            // A graph is: { "nodeA": ["nodeB", "nodeC"], "nodeB": ["nodeD"] }
            // Meaning: nodeA points to nodeB and nodeC
            //          nodeB points to nodeD
            
            var graph = new Dictionary<string, List<string>>();
            
            // STEP 1: Initialize all nodes with empty lists
            foreach (var node in flow.Nodes)
            {
                
                if (node.Id != null)  // ← Add this check
                    {
                        graph[node.Id] = new List<string>();
                    }

            }
            
            // STEP 2: Add connections based on edges
            foreach (var edge in flow.Edges)
            {
                // Edge says: "from" node points to "to" node
                if (edge.From != null && edge.To != null && graph.ContainsKey(edge.From))  // ← Add null checks
                    {
                        graph[edge.From].Add(edge.To);
                    }
            }
            
            return graph;
        }
        
        /// <summary>
        /// Calculates the execution order for all nodes in a flow using topological sorting (Kahn's Algorithm)
        /// </summary>
        /// <param name="flow">The flow to analyze</param>
        /// <returns>
        /// List of node IDs in execution order (dependencies first, then dependents)
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the flow contains a cycle (circular dependency), making topological sort impossible
        /// </exception>
        /// <remarks>
        /// <para>
        /// <b>Algorithm: Kahn's Topological Sort</b>
        /// </para>
        /// <para>
        /// <b>Steps:</b>
        /// <list type="number">
        ///   <item><description><b>Count in-degrees:</b> For each node, count how many edges point TO it</description></item>
        ///   <item><description><b>Queue start nodes:</b> Add all nodes with in-degree = 0 to queue</description></item>
        ///   <item><description><b>Process queue:</b> Remove node, add to result, reduce neighbors' in-degrees</description></item>
        ///   <item><description><b>Queue freed nodes:</b> If neighbor's in-degree reaches 0, add to queue</description></item>
        ///   <item><description><b>Validate:</b> If result.Count != nodes.Count, a cycle exists!</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Time Complexity:</b> O(V + E) where V = nodes, E = edges
        /// </para>
        /// </remarks>
        /// <example>
        /// Example flow:
        /// <code>
        /// Input → Orchestrator → Department → Formatter
        ///
        /// var executor = new DAGExecutor();
        /// var order = executor.GetExecutionOrder(flow);
        /// // Returns: ["Input", "Orchestrator", "Department", "Formatter"]
        /// </code>
        /// </example>
        public List<string> GetExecutionOrder(Flow flow)
        {
            // Build the graph first
            var graph = BuildGraph(flow);
            
            // COUNT: How many edges point TO each node?
            // This is called "in-degree"
            var inDegree = new Dictionary<string, int>();
            
            // Initialize: Start with 0 for all nodes
            foreach (var node in flow.Nodes)
            {
                if (node.Id != null)  // ← Add this check
                    {
                        inDegree[node.Id] = 0;
                    }
            }
            
            // Count incoming edges
            foreach (var edge in flow.Edges)
            {
                if (edge.To != null && inDegree.ContainsKey(edge.To))
                {
                    inDegree[edge.To]++;  // One more edge points to this node
                }
            }
            
            // QUEUE: Nodes with NO dependencies (in-degree = 0)
            // These can run FIRST!
            var queue = new Queue<string>();
            
            foreach (var kvp in inDegree)
            {
                if (kvp.Value == 0)
                {
                    queue.Enqueue(kvp.Key);  // Add to queue
                }
            }
            
            // RESULT: Will store the final execution order
            var result = new List<string>();
            
            // PROCESS: Remove nodes from queue one by one
            while (queue.Count > 0)
            {
                // Take a node from the queue
                var current = queue.Dequeue();
                result.Add(current);  // Add to result
                
                // For each node that THIS node points to:
                if (graph.ContainsKey(current))
                {
                    foreach (var neighbor in graph[current])
                    {
                        // Reduce its in-degree (one dependency is satisfied)
                        inDegree[neighbor]--;
                        
                        // If all dependencies are satisfied (in-degree = 0):
                        if (inDegree[neighbor] == 0)
                        {
                            queue.Enqueue(neighbor);  // Ready to execute!
                        }
                    }
                }
            }
            
            // CHECK: Did we process all nodes?
            if (result.Count != flow.Nodes.Count)
            {
                // If not, there's a CYCLE (circular dependency)!
                throw new InvalidOperationException(
                    "Flow contains a cycle! Cannot determine execution order."
                );
            }
            
            return result;
        }
        
        /// <summary>
        /// Gathers all inputs for a specific node by collecting outputs from its predecessor nodes
        /// </summary>
        /// <param name="nodeId">The ID of the node to get inputs for</param>
        /// <param name="flow">The flow containing the node and edge definitions</param>
        /// <param name="context">The execution context containing outputs from previously executed nodes</param>
        /// <returns>
        /// Dictionary of input key-value pairs for the node. Keys come from edge.ToInput, values from predecessor outputs.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method examines all edges that point TO the specified node, retrieves the outputs from
        /// the source nodes, and maps them to the input parameters expected by the target node.
        /// </para>
        /// <para>
        /// <b>Edge Mapping:</b>
        /// <code>
        /// Edge: from="node1", to="node2", fromOutput="result", toInput="data"
        ///
        /// Result: inputs["data"] = context.NodeOutputs["node1"]["result"]
        /// </code>
        /// </para>
        /// <para>
        /// <b>Handling Multiple Inputs:</b>
        /// If multiple edges map to the same input key, values are combined into a List&lt;object&gt;.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var executor = new DAGExecutor();
        /// var inputs = executor.GetNodeInputs("formatter", flow, context);
        /// // Returns: { "tech_response": "...", "billing_response": "...", ... }
        /// </code>
        /// </example>
        public Dictionary<string, object> GetNodeInputs(
            string nodeId,
            Flow flow,
            FlowManager.Core.Models.ExecutionContext context)
        {
            // This will store all inputs for the node
            var inputs = new Dictionary<string, object>();
            
            // FIND: All edges that point TO this node
            var incomingEdges = flow.Edges
                .Where(e => e.To == nodeId)
                .ToList();
            
            // ⭐ DEBUG ⭐
            Console.WriteLine($"[DAGExecutor] Getting inputs for node '{nodeId}'");
            Console.WriteLine($"[DAGExecutor] Found {incomingEdges.Count} incoming edges");
            
            // For each incoming edge:
            foreach (var edge in incomingEdges)
            {
                Console.WriteLine($"[DAGExecutor] Processing edge: {edge.From} -> {edge.To}");
                Console.WriteLine($"[DAGExecutor]   FromOutput: '{edge.FromOutput}', ToInput: '{edge.ToInput}'");
                
                if (edge.From != null && context.NodeOutputs.ContainsKey(edge.From))
                {
                    var sourceOutputs = context.NodeOutputs[edge.From];
                    Console.WriteLine($"[DAGExecutor]   Source node '{edge.From}' has {sourceOutputs.Count} outputs");
                    
                    // Use the edge's FromOutput (defaults to "output")
                    var fromOutput = edge.FromOutput;
                    
                    // Use the edge's ToInput (defaults to "input")  
                    var toInput = edge.ToInput;
                    
                    // If the source has this output, add it to inputs
                    if (sourceOutputs.ContainsKey(fromOutput))
                    {
                        var value = sourceOutputs[fromOutput];
                        Console.WriteLine($"[DAGExecutor]   ✅ Mapping: outputs['{fromOutput}'] = '{value}' -> inputs['{toInput}']");

                        // ✨ FIX: If toInput key already exists, combine values into a list
                        if (inputs.ContainsKey(toInput))
                        {
                            Console.WriteLine($"[DAGExecutor]   ⚠️  Key '{toInput}' already exists, combining values into list");

                            // Convert existing value to list if it isn't already
                            if (inputs[toInput] is List<object> existingList)
                            {
                                existingList.Add(value);
                            }
                            else
                            {
                                // Create new list with existing value and new value
                                var list = new List<object> { inputs[toInput], value };
                                inputs[toInput] = list;
                            }
                        }
                        else
                        {
                            // First value for this key
                            inputs[toInput] = value;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[DAGExecutor]   ❌ Source node doesn't have output '{fromOutput}'");
                        Console.WriteLine($"[DAGExecutor]      Available outputs: {string.Join(", ", sourceOutputs.Keys)}");
                    }
                }
                else
                {
                    Console.WriteLine($"[DAGExecutor]   ❌ Source node '{edge.From}' not found in context");
                }
            }
            
            Console.WriteLine($"[DAGExecutor] Final inputs for '{nodeId}': {inputs.Count} items");
            foreach (var kvp in inputs)
            {
                Console.WriteLine($"[DAGExecutor]   - {kvp.Key}: {kvp.Value}");
            }
            Console.WriteLine();
            
            return inputs;
        }

        /// <summary>
        /// Identifies start nodes (entry points) in a flow - nodes with no incoming edges
        /// </summary>
        /// <param name="flow">The flow to analyze</param>
        /// <returns>
        /// List of node IDs that have no dependencies (no incoming edges). These nodes can execute immediately.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no start nodes are found (every node has incoming edges), indicating a cycle or invalid flow structure
        /// </exception>
        /// <remarks>
        /// <para>
        /// Start nodes are the entry points to a flow - they have no dependencies and can execute immediately.
        /// Typically, flows have one start node (like an Input node), but parallel flows may have multiple.
        /// </para>
        /// <para>
        /// <b>Algorithm:</b>
        /// <list type="number">
        ///   <item><description>Collect all node IDs that appear in edge.To (nodes with incoming edges)</description></item>
        ///   <item><description>Find nodes NOT in that set (nodes with no incoming edges)</description></item>
        ///   <item><description>These are the start nodes</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Validation:</b> If zero start nodes found, the flow is invalid (cycle or all nodes have dependencies).
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var executor = new DAGExecutor();
        /// var startNodes = executor.GetStartNodes(flow);
        /// // Returns: ["input1"] (most common)
        /// // Or: ["input1", "config_loader"] (parallel start points)
        /// </code>
        /// </example>
        public List<string> GetStartNodes(Flow flow)
        {
            // STEP 1: Find all nodes that have incoming edges
            var nodesWithIncoming = new HashSet<string>();

            foreach (var edge in flow.Edges)
            {
                if (edge.To != null)
                {
                    nodesWithIncoming.Add(edge.To);
                }
            }

            // STEP 2: Find nodes that are NOT in that set
            // These are the start nodes (no dependencies)
            var startNodes = new List<string>();

            foreach (var node in flow.Nodes)
            {
                if (node.Id != null && !nodesWithIncoming.Contains(node.Id))
                {
                    startNodes.Add(node.Id);
                }
            }

            Console.WriteLine($"[DAGExecutor] Found {startNodes.Count} start node(s): {string.Join(", ", startNodes)}");

            // STEP 3: Validate - must have at least one start node
            if (startNodes.Count == 0)
            {
                throw new InvalidOperationException(
                    "Flow has no start nodes! Every node has incoming edges. " +
                    "This creates a cycle or an invalid flow structure."
                );
            }

            return startNodes;
        }
    }
}