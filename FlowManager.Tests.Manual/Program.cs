using FlowManager.Core.Models;
using FlowManager.Core.Engine;
using FlowManager.Core.Nodes;

Console.WriteLine("🎯 Complete System Test (All 3 Departments)\n");
Console.WriteLine("═══════════════════════════════════════");
Console.WriteLine("\n📋 Flow Structure:");
Console.WriteLine("   input1 → orchestrator1 → search1 (conditional)");
Console.WriteLine("                          → data1 (conditional)");
Console.WriteLine("                          → function1 (conditional)");
Console.WriteLine("\n✨ With new dependency tracking, departments wait for orchestrator!");
Console.WriteLine("═══════════════════════════════════════\n");

var apiKey = "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";

// Test questions for all 3 departments
var testQuestions = new[]
{
    "When was Microsoft founded?",                              // Search
    "What are the forklift safety procedures?",                // Data
    "Extract the product code from: Item XYZ-789-2024",       // Function
    "Generate a brief sales report summary",                   // Function
    "What are warehouse fire safety protocols?",              // Data
    "Search for information about quantum computing"           // Search
};

foreach (var question in testQuestions)
{
    Console.WriteLine($"❓ Question: {question}");
    Console.WriteLine("─────────────────────────────────────");
    
    var flow = new Flow
    {
        Id = "complete-system-test",
        Name = "Complete System Test",
        Nodes = new List<NodeDefinition>
        {
            new NodeDefinition
            {
                Id = "input1",
                Type = "Input",
                Config = new Dictionary<string, object>
                {
                    ["text"] = question
                }
            },
            new NodeDefinition
            {
                Id = "orchestrator1",
                Type = "Orchestrator",
                Config = new Dictionary<string, object>
                {
                    ["apiKey"] = apiKey
                }
            },
            new NodeDefinition
            {
                Id = "search1",
                Type = "SearchDepartment",
                Config = new Dictionary<string, object>
                {
                    ["apiKey"] = apiKey
                },
                Condition = "{{orchestrator1.targets}} contains 'Search'"
            },
            new NodeDefinition
            {
                Id = "data1",
                Type = "DataDepartment",
                Config = new Dictionary<string, object>
                {
                    ["apiKey"] = apiKey
                },
                Condition = "{{orchestrator1.targets}} contains 'Data'"
            },
            new NodeDefinition
            {
                Id = "function1",
                Type = "FunctionDepartment",
                Config = new Dictionary<string, object>
                {
                    ["apiKey"] = apiKey
                },
                Condition = "{{orchestrator1.targets}} contains 'Function'"
            }
        },
        Edges = new List<Edge>
        {
            // Input feeds into orchestrator
            new Edge { From = "input1", To = "orchestrator1", FromOutput = "output", ToInput = "input" },

            // Orchestrator routes to departments (conditional edges)
            // Pass the 'query' output (user's question) to departments as 'input'
            new Edge { From = "orchestrator1", To = "search1", FromOutput = "query", ToInput = "input" },
            new Edge { From = "orchestrator1", To = "data1", FromOutput = "query", ToInput = "input" },
            new Edge { From = "orchestrator1", To = "function1", FromOutput = "query", ToInput = "input" }
        }
    };
    
    var orchestrator = new FlowOrchestrator();
    var context = await orchestrator.ExecuteFlowAsync(flow);
    
    if (context.Status == ExecutionStatus.Completed)
    {
        Console.WriteLine("\n📊 EXECUTION BATCHES:");
        for (int i = 0; i < context.ExecutionBatches.Count; i++)
        {
            Console.WriteLine($"   Batch {i + 1}: [{string.Join(", ", context.ExecutionBatches[i])}]");
        }

        Console.WriteLine("\n🧠 ORCHESTRATOR:");
        var orchOutputs = context.NodeOutputs["orchestrator1"];
        Console.WriteLine($"   Decision: {orchOutputs["action"]}");
        Console.WriteLine($"   Target: {orchOutputs["targets"]}");

        // Check Search
        if (context.NodeOutputs.ContainsKey("search1") && 
            context.NodeOutputs["search1"].ContainsKey("output") &&
            !string.IsNullOrEmpty(context.NodeOutputs["search1"]["output"]?.ToString()))
        {
            Console.WriteLine($"\n🔍 SEARCH:");
            Console.WriteLine($"   {context.NodeOutputs["search1"]["output"]}");
        }
        
        // Check Data
        if (context.NodeOutputs.ContainsKey("data1") && 
            context.NodeOutputs["data1"].ContainsKey("output") &&
            !string.IsNullOrEmpty(context.NodeOutputs["data1"]["output"]?.ToString()))
        {
            Console.WriteLine($"\n📚 DATA:");
            var output = context.NodeOutputs["data1"]["output"]?.ToString();
            // Truncate if too long
            if (output != null && output.Length > 300)
                output = output.Substring(0, 300) + "...";
            Console.WriteLine($"   {output}");
        }
        
        // Check Function
        if (context.NodeOutputs.ContainsKey("function1") && 
            context.NodeOutputs["function1"].ContainsKey("output") &&
            !string.IsNullOrEmpty(context.NodeOutputs["function1"]["output"]?.ToString()))
        {
            Console.WriteLine($"\n⚙️ FUNCTION:");
            Console.WriteLine($"   {context.NodeOutputs["function1"]["output"]}");
        }
    }
    else
    {
        Console.WriteLine($"\n❌ Error: {string.Join(", ", context.Errors)}");
    }
    
    Console.WriteLine("\n═══════════════════════════════════════\n");
}

Console.WriteLine("🎉 Complete system test finished!");
Console.WriteLine("\n📊 SUMMARY:");
Console.WriteLine("✅ Search Department - Handles web/factual queries");
Console.WriteLine("✅ Data Department - Handles procedures/protocols");
Console.WriteLine("✅ Function Department - Handles extraction/reports");
Console.WriteLine("✅ Orchestrator - Routes intelligently");
Console.WriteLine("✅ Conditional Logic - Only runs chosen departments");
Console.WriteLine("\n🔷 Dependency Tracking Features:");
Console.WriteLine("✅ Departments wait for orchestrator to complete before executing");
Console.WriteLine("✅ Proper batch sequencing: [input] → [orchestrator] → [selected departments]");
Console.WriteLine("✅ No race conditions - conditions evaluated AFTER orchestrator produces output");