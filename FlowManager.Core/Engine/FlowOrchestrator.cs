using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlowManager.Core.Models;
using FlowManager.Core.Nodes;
using FlowManager.Core.Services.KnowledgeBase;
using ExecutionContext = FlowManager.Core.Models.ExecutionContext;

namespace FlowManager.Core.Engine
{
    /// <summary>
    /// Flow Orchestrator - The central conductor that orchestrates complete flow execution
    /// </summary>
    /// <remarks>
    /// <para>
    /// FlowOrchestrator is the "brain" of the FlowManager system. It coordinates all components
    /// (DAGExecutor, VariableInterpolator, EdgeEvaluator, ConditionEvaluator) to execute flows
    /// with dynamic routing, parallel execution, conditional branching, and retry logic.
    /// </para>
    /// <para>
    /// <b>Key Responsibilities:</b>
    /// <list type="bullet">
    ///   <item><description><b>Flow Validation:</b> Uses Kahn's Algorithm to detect cycles before execution</description></item>
    ///   <item><description><b>Dynamic Execution:</b> Executes nodes based on edge conditions evaluated at runtime</description></item>
    ///   <item><description><b>Parallel Batching:</b> Executes independent nodes simultaneously for performance</description></item>
    ///   <item><description><b>Dependency Management:</b> Tracks and enforces node dependencies (in-degree tracking)</description></item>
    ///   <item><description><b>Variable Interpolation:</b> Replaces {{nodeId.output}} in configs with actual values</description></item>
    ///   <item><description><b>Edge Evaluation:</b> Determines which edges to follow based on conditions</description></item>
    ///   <item><description><b>Retry Logic:</b> Automatically retries failed nodes with exponential backoff</description></item>
    ///   <item><description><b>Conversation Context:</b> Provides conversation history to nodes for context-aware processing</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Execution Flow (5 Steps):</b>
    /// <list type="number">
    ///   <item><description><b>Step 1:</b> Create ExecutionContext to track runtime state</description></item>
    ///   <item><description><b>Step 2:</b> Validate flow structure (cycle detection using Kahn's Algorithm)</description></item>
    ///   <item><description><b>Step 3:</b> Initialize execution tracking (queue, executed set, dependency counts)</description></item>
    ///   <item><description><b>Step 4:</b> Find start nodes (nodes with no incoming edges)</description></item>
    ///   <item><description><b>Step 5:</b> Dynamic execution loop with parallel batching:
    ///     <list type="bullet">
    ///       <item><description>Dequeue all ready nodes as a batch</description></item>
    ///       <item><description>Execute batch in parallel with Task.WhenAll</description></item>
    ///       <item><description>Evaluate outgoing edges and update dependency counts</description></item>
    ///       <item><description>Queue downstream nodes when dependencies satisfied</description></item>
    ///       <item><description>Repeat until queue is empty</description></item>
    ///     </list>
    ///   </description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Parallel Execution:</b>
    /// <br/>
    /// Nodes in the same batch execute simultaneously via Task.WhenAll. ExecutionBatches property
    /// tracks which nodes ran in parallel for visualization and performance analysis.
    /// </para>
    /// <para>
    /// <b>Conditional Branching:</b>
    /// <br/>
    /// Edge conditions are evaluated AFTER node execution. Only edges with true conditions are
    /// followed. Nodes with no satisfied incoming edges are skipped and their children unblocked.
    /// </para>
    /// <para>
    /// <b>Retry Mechanism:</b>
    /// <br/>
    /// Failed nodes are retried up to maxRetries times with exponential backoff (2^attempt seconds).
    /// If all retries fail, flow execution stops with ExecutionStatus.Failed.
    /// </para>
    /// <para>
    /// <b>Node Type Registry:</b>
    /// <br/>
    /// Maintains mapping of node type names to C# classes. Supports: Input, PromptTemplate,
    /// OpenAIChat, Transform, Orchestrator, SearchDepartment, DataDepartment, FunctionDepartment,
    /// ImageDepartment, Formatter.
    /// </para>
    /// </remarks>
    /// <example>
    /// Basic usage:
    /// <code>
    /// var orchestrator = new FlowOrchestrator();
    /// var flow = LoadFlow("my-flow");
    /// var context = await orchestrator.ExecuteFlowAsync(flow);
    ///
    /// // Check results
    /// if (context.Status == ExecutionStatus.Completed)
    /// {
    ///     var output = context.NodeOutputs["formatter"]["output"];
    /// }
    /// </code>
    ///
    /// With conversation context:
    /// <code>
    /// var orchestrator = new FlowOrchestrator(knowledgeBase, webSearch);
    /// var conversation = new ConversationMemory();
    /// conversation.AddUserMessage("What are forklift safety procedures?");
    ///
    /// var context = await orchestrator.ExecuteFlowAsync(flow, maxRetries: 3, conversation);
    ///
    /// // Conversation history is available to all nodes
    /// // OrchestratorNode can use it to understand follow-up questions
    /// </code>
    /// </example>
    public class FlowOrchestrator
    {
        // DAG Executor - figures out execution order
        private readonly DAGExecutor _dagExecutor;

        // Variable Interpolator - replaces {{nodeId.output}} with values
        private readonly VariableInterpolator _interpolator;

        // Condition Evaluator - checks if nodes should run
        private readonly ConditionEvaluator _conditionEvaluator;

        // Edge Evaluator - checks if edges should be followed
        private readonly EdgeEvaluator _edgeEvaluator;

        // Node Type Registry - maps type names to C# classes
        private readonly Dictionary<string, Type> _nodeTypes;

        // Knowledge Base services for SearchDepartment
        private readonly IKnowledgeBaseService? _knowledgeBaseService;
        private readonly IWebSearchService? _webSearchService;

        // Variable Interpolator - replaces {{nodeId.output}} with values

        /// <summary>
        /// Initializes a new instance of FlowOrchestrator with optional external services
        /// </summary>
        /// <param name="knowledgeBaseService">
        /// Optional knowledge base service for SearchDepartmentNode to query internal documents.
        /// If null, SearchDepartmentNode will only use web search (if available).
        /// </param>
        /// <param name="webSearchService">
        /// Optional web search service for SearchDepartmentNode to query external sources.
        /// If null, SearchDepartmentNode will only use knowledge base (if available).
        /// </param>
        /// <remarks>
        /// <para>
        /// Initializes all core components:
        /// <list type="bullet">
        ///   <item><description><b>DAGExecutor:</b> For topological sorting and dependency management</description></item>
        ///   <item><description><b>VariableInterpolator:</b> For {{nodeId.output}} substitution</description></item>
        ///   <item><description><b>ConditionEvaluator:</b> For node-level condition evaluation</description></item>
        ///   <item><description><b>EdgeEvaluator:</b> For edge-level condition evaluation</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Registers all supported node types:
        /// Input, PromptTemplate, OpenAIChat, Transform, Orchestrator, SearchDepartment,
        /// DataDepartment, FunctionDepartment, ImageDepartment, Formatter
        /// </para>
        /// </remarks>
        public FlowOrchestrator(
            IKnowledgeBaseService? knowledgeBaseService = null,
            IWebSearchService? webSearchService = null)
        {
            _dagExecutor = new DAGExecutor();
            _interpolator = new VariableInterpolator();
            _conditionEvaluator = new ConditionEvaluator();
            _edgeEvaluator = new EdgeEvaluator();  // ✨ NEW
            _knowledgeBaseService = knowledgeBaseService;
            _webSearchService = webSearchService;  
            
            // REGISTER ALL NODE TYPES
            // This tells the orchestrator: "When you see 'Input', create InputNode"
            _nodeTypes = new Dictionary<string, Type>
            {
                { "Input", typeof(InputNode) },
                { "PromptTemplate", typeof(PromptTemplateNode) },
                { "OpenAIChat", typeof(OpenAIChatNode) },
                { "Transform", typeof(TransformNode) },
                { "Orchestrator", typeof(OrchestratorNode) },
                { "SearchDepartment", typeof(SearchDepartmentNode) },
                { "DataDepartment", typeof(DataDepartmentNode) },
                { "FunctionDepartment", typeof(FunctionDepartmentNode) } ,
                { "ImageDepartment", typeof(ImageDepartmentNode) },      // ← ADD
                { "Formatter", typeof(FormatterNode) }  

            };
        }

        /// <summary>
        /// Executes a complete flow with dynamic routing, parallel execution, and retry logic
        /// </summary>
        /// <param name="flow">
        /// The flow definition containing nodes and edges to execute.
        /// Must have valid structure (no cycles, valid node types, connected graph).
        /// </param>
        /// <param name="maxRetries">
        /// Maximum number of retry attempts for failed nodes (default: 3).
        /// Uses exponential backoff: 2^attempt seconds between retries.
        /// Example: 1st retry after 2s, 2nd after 4s, 3rd after 8s.
        /// </param>
        /// <param name="conversationMemory">
        /// Optional conversation context providing history for context-aware processing.
        /// If provided, conversation history is copied into ExecutionContext and made available
        /// to all nodes during execution. OrchestratorNode uses this to understand follow-up questions.
        /// </param>
        /// <returns>
        /// ExecutionContext containing complete execution results:
        /// <list type="bullet">
        ///   <item><description><b>Status:</b> Completed, Failed, or Cancelled</description></item>
        ///   <item><description><b>NodeOutputs:</b> Dictionary of all node outputs (nodeId → outputName → value)</description></item>
        ///   <item><description><b>ExecutionOrder:</b> Sequential list of executed nodes</description></item>
        ///   <item><description><b>ExecutionBatches:</b> List of parallel execution batches</description></item>
        ///   <item><description><b>Errors:</b> List of error messages if any occurred</description></item>
        ///   <item><description><b>StartTime/EndTime:</b> Execution timestamps for performance analysis</description></item>
        /// </list>
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown when:
        /// <list type="bullet">
        ///   <item><description>Flow contains cycles (detected by Kahn's Algorithm validation)</description></item>
        ///   <item><description>Node not found in flow definition</description></item>
        ///   <item><description>Unknown node type encountered</description></item>
        ///   <item><description>Node execution fails after all retry attempts exhausted</description></item>
        /// </list>
        /// </exception>
        /// <remarks>
        /// <para>
        /// <b>Execution Algorithm:</b>
        /// </para>
        /// <para>
        /// <b>Phase 1: Validation</b>
        /// <br/>
        /// Uses DAGExecutor.GetExecutionOrder() to validate flow structure via Kahn's Algorithm.
        /// This ensures no cycles exist before execution begins.
        /// </para>
        /// <para>
        /// <b>Phase 2: Initialization</b>
        /// <br/>
        /// - Creates ExecutionContext with unique ExecutionId and StartTime
        /// <br/>
        /// - Copies conversation history from ConversationMemory (if provided)
        /// <br/>
        /// - Initializes execution queue, executed set, and dependency tracking
        /// <br/>
        /// - Calculates in-degree (dependency count) for each node
        /// <br/>
        /// - Finds and queues start nodes (nodes with no incoming edges)
        /// </para>
        /// <para>
        /// <b>Phase 3: Dynamic Execution Loop</b>
        /// <br/>
        /// Executes in batches until queue is empty:
        /// <list type="number">
        ///   <item><description><b>Dequeue Batch:</b> Take all currently queued nodes as a batch</description></item>
        ///   <item><description><b>Check Conditions:</b> Evaluate node-level conditions, skip nodes with unmet conditions</description></item>
        ///   <item><description><b>Parallel Execution:</b> Execute all batch nodes simultaneously using Task.WhenAll</description></item>
        ///   <item><description><b>Variable Interpolation:</b> Replace {{nodeId.output}} in configs before execution</description></item>
        ///   <item><description><b>Retry Logic:</b> Retry failed nodes up to maxRetries times with exponential backoff</description></item>
        ///   <item><description><b>Store Outputs:</b> Save node outputs in ExecutionContext.NodeOutputs</description></item>
        ///   <item><description><b>Evaluate Edges:</b> Check conditions on outgoing edges, follow edges with true conditions</description></item>
        ///   <item><description><b>Update Dependencies:</b> Decrement in-degree for downstream nodes</description></item>
        ///   <item><description><b>Queue Ready Nodes:</b> Add nodes with zero dependencies to queue</description></item>
        ///   <item><description><b>Handle Skipped Nodes:</b> Unblock children of completely skipped nodes</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Phase 4: Completion</b>
        /// <br/>
        /// - Sets ExecutionContext.Status to Completed or Failed
        /// <br/>
        /// - Sets EndTime
        /// <br/>
        /// - Returns ExecutionContext with complete results
        /// </para>
        /// <para>
        /// <b>Parallel Execution Details:</b>
        /// <br/>
        /// Nodes in the same batch have no dependencies on each other and execute simultaneously.
        /// ExecutionBatches tracks which nodes ran in parallel for performance analysis.
        /// Example: [["input1"], ["orchestrator"], ["search","data","function"], ["formatter"]]
        /// </para>
        /// <para>
        /// <b>Conditional Branching:</b>
        /// <br/>
        /// Edge conditions are evaluated AFTER source node completes. Only edges with true conditions
        /// are followed. If a node has multiple outgoing edges, multiple paths may be taken simultaneously.
        /// Nodes with no satisfied incoming edges are marked as skipped with empty output.
        /// </para>
        /// <para>
        /// <b>Dependency Management:</b>
        /// <br/>
        /// Uses in-degree tracking (remaining dependencies count). A node executes only when ALL
        /// parent nodes have completed AND edge conditions are true. Decrements in-degree as edges
        /// are followed. Queues node when in-degree reaches zero.
        /// </para>
        /// <para>
        /// <b>Error Handling:</b>
        /// <br/>
        /// Failed nodes are retried with exponential backoff. If all retries fail, execution stops
        /// immediately with ExecutionStatus.Failed and error message in Errors list.
        /// </para>
        /// </remarks>
        /// <example>
        /// Basic flow execution:
        /// <code>
        /// var orchestrator = new FlowOrchestrator();
        /// var flow = new Flow
        /// {
        ///     Id = "example-flow",
        ///     Nodes = new List&lt;NodeDefinition&gt;
        ///     {
        ///         new NodeDefinition { Id = "input1", Type = "Input" },
        ///         new NodeDefinition { Id = "llm1", Type = "OpenAIChat", Config = new() { ["prompt"] = "{{input1.output}}" } }
        ///     },
        ///     Edges = new List&lt;Edge&gt;
        ///     {
        ///         new Edge { From = "input1", To = "llm1" }
        ///     }
        /// };
        ///
        /// var context = await orchestrator.ExecuteFlowAsync(flow);
        ///
        /// if (context.Status == ExecutionStatus.Completed)
        /// {
        ///     var result = context.NodeOutputs["llm1"]["output"];
        ///     Console.WriteLine($"Result: {result}");
        /// }
        /// </code>
        ///
        /// With retry and conversation context:
        /// <code>
        /// var conversation = new ConversationMemory();
        /// conversation.AddUserMessage("What are the safety procedures?");
        ///
        /// var context = await orchestrator.ExecuteFlowAsync(
        ///     flow,
        ///     maxRetries: 5,  // Retry up to 5 times
        ///     conversationMemory: conversation
        /// );
        ///
        /// // Check parallel execution batches
        /// foreach (var batch in context.ExecutionBatches)
        /// {
        ///     Console.WriteLine($"Batch: {string.Join(", ", batch)}");
        /// }
        /// </code>
        ///
        /// Conditional branching flow:
        /// <code>
        /// var flow = new Flow
        /// {
        ///     Nodes = new List&lt;NodeDefinition&gt;
        ///     {
        ///         new NodeDefinition { Id = "orchestrator", Type = "Orchestrator" },
        ///         new NodeDefinition { Id = "search", Type = "SearchDepartment" },
        ///         new NodeDefinition { Id = "data", Type = "DataDepartment" }
        ///     },
        ///     Edges = new List&lt;Edge&gt;
        ///     {
        ///         new Edge { From = "orchestrator", To = "search", Condition = "{{orchestrator.targets}} contains 'Search'" },
        ///         new Edge { From = "orchestrator", To = "data", Condition = "{{orchestrator.targets}} contains 'Data'" }
        ///     }
        /// };
        ///
        /// // Orchestrator decides which departments to route to
        /// // Only edges with true conditions are followed
        /// var context = await orchestrator.ExecuteFlowAsync(flow);
        /// </code>
        /// </example>
        public async Task<ExecutionContext> ExecuteFlowAsync(
            Flow flow,
            int maxRetries = 3,
            ConversationMemory? conversationMemory = null)
        {
            // STEP 1: Create execution context to track everything
            var context = new ExecutionContext
            {
                FlowId = flow.Id,
                StartTime = DateTime.UtcNow,
                Status = ExecutionStatus.Running
            };

            // Copy conversation history into ExecutionContext
            // This makes the conversation history available to ALL nodes during execution
            // Nodes like OrchestratorNode can read this to understand follow-up questions
            if (conversationMemory != null && conversationMemory.Messages.Count > 0)
            {
                context.ConversationHistory = new List<ConversationMessage>(conversationMemory.Messages);
                Console.WriteLine($"[FlowOrchestrator] 📝 Loaded {conversationMemory.Messages.Count} messages from conversation history");
            }
            
            try
            {
                // STEP 2: Validate flow structure (still use Kahn's Algorithm for cycle detection)
                var validationOrder = _dagExecutor.GetExecutionOrder(flow);
                Console.WriteLine($"[FlowOrchestrator] ✅ Flow validation passed (no cycles detected)");

                // DYNAMIC EXECUTION with WHILE LOOP
                // Instead of pre-calculating order, we build it as we go based on edge conditions

                // STEP 3: Initialize execution tracking
                var queue = new Queue<string>();
                var executed = new HashSet<string>();
                var queued = new HashSet<string>();  // ✨ Track what's already in queue to prevent duplicates
                var executionOrder = new List<string>();

                //STEP 3.5: Initialize in-degree tracking for dependency management
                // This ensures nodes only execute when ALL parent nodes have completed
                var remainingDependencies = new Dictionary<string, int>();

                foreach (var node in flow.Nodes)
                {
                    if (node.Id != null)
                    {
                        // Count how many incoming edges (dependencies) this node has
                        int incomingCount = flow.Edges.Count(e => e.To == node.Id);
                        remainingDependencies[node.Id] = incomingCount;

                        if (incomingCount > 0)
                        {
                            Console.WriteLine($"[FlowOrchestrator] 📊 Node '{node.Id}' has {incomingCount} parent(s)");
                        }
                    }
                }

                // STEP 4: Find start nodes (nodes with no incoming edges)
                var startNodes = _dagExecutor.GetStartNodes(flow);

                foreach (var startNode in startNodes)
                {
                    queue.Enqueue(startNode);
                    queued.Add(startNode);  // ✨ Mark as queued
                }

                Console.WriteLine($"[FlowOrchestrator] 🚀 Starting dynamic execution with {startNodes.Count} entry point(s)");

                // STEP 5: DYNAMIC EXECUTION LOOP WITH PARALLEL BATCHING
                // Execute nodes in batches - all nodes in the queue with satisfied dependencies run in parallel
                int batchNumber = 0;
                while (queue.Count > 0)
                {
                    batchNumber++;

                    // PARALLEL EXECUTION: Take ALL nodes currently in queue as a batch
                    var batch = new List<string>();
                    var queueSnapshot = queue.Count;

                    for (int i = 0; i < queueSnapshot; i++)
                    {
                        var nodeId = queue.Dequeue();
                        queued.Remove(nodeId);  // ✨ Remove from queued set

                        // Skip if already executed (prevent duplicate execution)
                        if (executed.Contains(nodeId))
                        {
                            Console.WriteLine($"[FlowOrchestrator] ⏭️  Skipping {nodeId} (already executed)");
                            continue;
                        }

                        batch.Add(nodeId);
                    }

                    if (batch.Count == 0)
                    {
                        continue;  // No nodes to execute in this batch
                    }

                    Console.WriteLine($"[FlowOrchestrator] 🔄 BATCH {batchNumber}: Executing {batch.Count} node(s) in parallel: [{string.Join(", ", batch)}]");

                    // Execute all nodes in batch in parallel
                    var batchTasks = batch.Select(async nodeId =>
                    {
                        // Find the node definition in the flow
                        var nodeDefinition = flow.Nodes.FirstOrDefault(n => n.Id == nodeId);

                        if (nodeDefinition == null)
                        {
                            throw new Exception($"Node {nodeId} not found in flow");
                        }

                        // Check if node should run based on its own condition
                        if (!_conditionEvaluator.ShouldNodeRun(nodeDefinition, context))
                        {
                            Console.WriteLine($"[FlowOrchestrator] ⏭️  Skipping node {nodeId} (node condition not met)");

                            // Add empty output so downstream nodes don't break
                            context.NodeOutputs[nodeId] = new Dictionary<string, object>();
                            executed.Add(nodeId);

                            return nodeId;  // Return nodeId to indicate completion
                        }

                        Console.WriteLine($"[FlowOrchestrator] ▶️  Executing node {nodeId}");

                        // Create the actual node instance
                        var node = CreateNodeInstance(nodeDefinition);

                        // Get inputs from previous nodes
                        var inputs = _dagExecutor.GetNodeInputs(nodeId, flow, context);

                        // Interpolate variables in the node's config
                        node.Config = _interpolator.InterpolateConfig(node.Config, context);

                        // Provide ExecutionContext to the node
                        var contextProperty = node.GetType().BaseType?.GetProperty("Context",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        contextProperty?.SetValue(node, context);

                        // Execute the node WITH RETRY logic
                        var success = false;
                        var attempts = 0;
                        Exception? lastException = null;

                        while (!success && attempts < maxRetries)
                        {
                            try
                            {
                                attempts++;

                                // ACTUALLY EXECUTE THE NODE!
                                var outputs = await node.ExecuteAsync(inputs);

                                // Store outputs in context for next nodes
                                context.NodeOutputs[nodeId] = outputs;

                                success = true;
                            }
                            catch (Exception ex)
                            {
                                lastException = ex;

                                // If we have retries left, wait and try again
                                if (attempts < maxRetries)
                                {
                                    // Exponential backoff: 2^attempts seconds
                                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempts));
                                    await Task.Delay(delay);
                                }
                            }
                        }

                        // If all retries failed, mark as failed and stop
                        if (!success)
                        {
                            context.Status = ExecutionStatus.Failed;
                            var errorMsg = $"Node {nodeId} failed after {maxRetries} attempts: {lastException?.Message}";
                            context.Errors.Add(errorMsg);
                            throw new Exception(errorMsg, lastException);
                        }

                        return nodeId;  // Return nodeId to indicate successful completion
                    }).ToList();

                    // WAIT FOR ALL NODES IN BATCH TO COMPLETE
                    await Task.WhenAll(batchTasks);

                    Console.WriteLine($"[FlowOrchestrator] ✅ BATCH {batchNumber} completed");

                    //  Mark all nodes in batch as executed (after parallel execution completes)
                    foreach (var nodeId in batch)
                    {
                        executed.Add(nodeId);
                        executionOrder.Add(nodeId);
                    }

                    // Track batch for visualization (shows which nodes ran in parallel)
                    context.ExecutionBatches.Add(new List<string>(batch));

                    // EVALUATE EDGES FOR ALL NODES IN BATCH
                    // After batch completes, check which edges to follow
                    var skippedNodes = new HashSet<string>();  // Track nodes that won't be queued

                    foreach (var nodeId in batch)
                    {
                        // EVALUATE OUTGOING EDGES - This is the magic!
                        // After executing this node, check which edges to follow
                        var outgoingEdges = flow.Edges.Where(e => e.From == nodeId).ToList();

                        Console.WriteLine($"[FlowOrchestrator] 🔍 Checking {outgoingEdges.Count} outgoing edge(s) from {nodeId}");

                        foreach (var edge in outgoingEdges)
                        {
                            // Evaluate edge condition
                            bool shouldFollow = _edgeEvaluator.ShouldFollowEdge(edge, context);

                            if (shouldFollow)
                            {
                                // FOLLOW THIS EDGE: Decrement and potentially queue
                                if (edge.To != null && !executed.Contains(edge.To))
                                {
                                    if (remainingDependencies.ContainsKey(edge.To))
                                    {
                                        remainingDependencies[edge.To]--;
                                        Console.WriteLine($"[FlowOrchestrator]   📉 {edge.To} dependencies: {remainingDependencies[edge.To] + 1} → {remainingDependencies[edge.To]} (edge followed)");

                                        if (remainingDependencies[edge.To] == 0 && !queued.Contains(edge.To))
                                        {
                                            queue.Enqueue(edge.To);
                                            queued.Add(edge.To);
                                            Console.WriteLine($"[FlowOrchestrator]   ✅ All dependencies satisfied! Added {edge.To} to queue");
                                        }
                                        else if (remainingDependencies[edge.To] > 0)
                                        {
                                            Console.WriteLine($"[FlowOrchestrator]   ⏳ {edge.To} waiting for {remainingDependencies[edge.To]} more parent(s)");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // SKIP THIS EDGE: Mark target as potentially skipped
                                if (edge.To != null && !executed.Contains(edge.To) && !queued.Contains(edge.To))
                                {
                                    skippedNodes.Add(edge.To);
                                    Console.WriteLine($"[FlowOrchestrator]   ⏭️  Skipping edge to {edge.To} (condition not met)");
                                }
                            }
                        }
                    }

                    // HANDLE SKIPPED NODES: Unblock their children
                    // Process nodes that won't be queued due to false edge conditions
                    foreach (var skippedNode in skippedNodes)
                    {
                        // Check if this node has no followed edges (completely skipped)
                        var hasFollowedEdge = flow.Edges.Any(e =>
                            e.To == skippedNode &&
                            executed.Contains(e.From) &&
                            _edgeEvaluator.ShouldFollowEdge(e, context));

                        if (!hasFollowedEdge && !queued.Contains(skippedNode))
                        {
                            // This node will never be queued - unblock its children
                            Console.WriteLine($"[FlowOrchestrator] ⚠️  Node {skippedNode} completely skipped, unblocking children");
                            executed.Add(skippedNode);  // Mark as executed (with no output)
                            context.NodeOutputs[skippedNode] = new Dictionary<string, object>();

                            // Evaluate its outgoing edges to unblock downstream nodes
                            var childEdges = flow.Edges.Where(e => e.From == skippedNode).ToList();
                            foreach (var childEdge in childEdges)
                            {
                                if (childEdge.To != null && !executed.Contains(childEdge.To))
                                {
                                    if (remainingDependencies.ContainsKey(childEdge.To))
                                    {
                                        remainingDependencies[childEdge.To]--;
                                        Console.WriteLine($"[FlowOrchestrator]   📉 {childEdge.To} dependencies: {remainingDependencies[childEdge.To] + 1} → {remainingDependencies[childEdge.To]} (parent skipped)");

                                        if (remainingDependencies[childEdge.To] == 0 && !queued.Contains(childEdge.To))
                                        {
                                            queue.Enqueue(childEdge.To);
                                            queued.Add(childEdge.To);
                                            Console.WriteLine($"[FlowOrchestrator]   ✅ Unblocked {childEdge.To}, added to queue");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // STEP 6: Store execution order in context
                context.ExecutionOrder = executionOrder;

                Console.WriteLine($"[FlowOrchestrator] ✅ Dynamic execution completed: {executed.Count} node(s) executed");
                Console.WriteLine($"[FlowOrchestrator]    Order: {string.Join(" → ", executionOrder)}");
                
                // STEP 4: All nodes executed successfully!
                context.Status = ExecutionStatus.Completed;
            }
            catch (Exception ex)
            {
                // Something went wrong
                context.Status = ExecutionStatus.Failed;
                
                // Add error if not already added
                if (!context.Errors.Any())
                {
                    context.Errors.Add(ex.Message);
                }
            }
            finally
            {
                // Always set end time
                context.EndTime = DateTime.UtcNow;
            }
            
            return context;
        }

        /// <summary>
        /// Creates a concrete node instance from a node definition using reflection
        /// </summary>
        /// <param name="definition">
        /// Node definition containing type, id, and configuration.
        /// Type must be registered in _nodeTypes dictionary.
        /// </param>
        /// <returns>
        /// Instantiated node with Id and Config properties set from definition
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when:
        /// <list type="bullet">
        ///   <item><description>Node type is null or empty</description></item>
        ///   <item><description>Node type is not registered in _nodeTypes</description></item>
        ///   <item><description>Failed to create instance via Activator.CreateInstance</description></item>
        /// </list>
        /// </exception>
        /// <remarks>
        /// <para>
        /// <b>Type Resolution:</b>
        /// <br/>
        /// Uses _nodeTypes dictionary to map type name (e.g., "OpenAIChat") to C# Type
        /// (e.g., typeof(OpenAIChatNode)). Then uses Activator.CreateInstance for instantiation.
        /// </para>
        /// <para>
        /// <b>Special Handling for SearchDepartmentNode:</b>
        /// <br/>
        /// SearchDepartmentNode requires constructor injection of IKnowledgeBaseService and
        /// IWebSearchService. These are injected from FlowOrchestrator's fields. All other
        /// node types use parameterless constructors.
        /// </para>
        /// <para>
        /// <b>Supported Node Types:</b>
        /// <br/>
        /// Input, PromptTemplate, OpenAIChat, Transform, Orchestrator, SearchDepartment,
        /// DataDepartment, FunctionDepartment, ImageDepartment, Formatter
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var definition = new NodeDefinition
        /// {
        ///     Id = "llm1",
        ///     Type = "OpenAIChat",
        ///     Config = new Dictionary&lt;string, object&gt;
        ///     {
        ///         ["prompt"] = "Hello, world!",
        ///         ["model"] = "gpt-4"
        ///     }
        /// };
        ///
        /// var node = CreateNodeInstance(definition);
        /// // Returns: OpenAIChatNode with Id="llm1" and Config set
        /// </code>
        /// </example>
        private Node CreateNodeInstance(NodeDefinition definition)
        {
            // Check if we know this node type
            if (string.IsNullOrEmpty(definition.Type) || !_nodeTypes.ContainsKey(definition.Type))
            {
                throw new InvalidOperationException(
                    $"Unknown node type: {definition.Type ?? "null"}. " +
                    $"Available types: {string.Join(", ", _nodeTypes.Keys)}"
                );
            }

            // Get the C# Type for this node
            var nodeType = _nodeTypes[definition.Type];

            //  NEW: Special handling for SearchDepartmentNode to inject services
            Node node;
            if (definition.Type == "SearchDepartment")
            {
                node = new SearchDepartmentNode(_knowledgeBaseService, _webSearchService);
            }
            else
            {
                // Create an instance using reflection
                var instance = Activator.CreateInstance(nodeType);
                if (instance == null)
                {
                    throw new InvalidOperationException($"Failed to create instance of type {nodeType.Name}");
                }
                node = (Node)instance;
            }

            // Set the properties
            node.Id = definition.Id;
            node.Config = new Dictionary<string, object>(definition.Config);
            
            return node;
        }
    }
}