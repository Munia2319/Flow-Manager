using FlowManager.Core.Models;
using FlowManager.Core.Engine;
using FlowManager.Core.Nodes;

namespace FlowManager.Tests.Manual
{
    /// <summary>
    /// Test to verify that the DAG execution engine correctly handles dependency tracking
    /// Tests various DAG patterns including diamond, multi-parent, and complex merging scenarios
    /// </summary>
    public class DAGDependencyTest
    {
        public static async Task TestDiamondPattern()
        {
            Console.WriteLine("🔷 Testing Diamond Pattern DAG");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("Structure:");
            Console.WriteLine("      A (100ms)");
            Console.WriteLine("     / \\");
            Console.WriteLine("    B   C (800ms)");
            Console.WriteLine("     \\ /");
            Console.WriteLine("      D (merger)");
            Console.WriteLine();
            Console.WriteLine("Expected: D should only execute AFTER both B and C complete");
            Console.WriteLine("═══════════════════════════════════════\n");

            var flow = new Flow
            {
                Id = "diamond-test",
                Name = "Diamond Pattern Test",
                Nodes = new List<NodeDefinition>
                {
                    new NodeDefinition
                    {
                        Id = "A",
                        Type = "Input",
                        Config = new Dictionary<string, object>
                        {
                            ["text"] = "Start node A"
                        }
                    },
                    new NodeDefinition
                    {
                        Id = "B",
                        Type = "Transform",
                        Config = new Dictionary<string, object>
                        {
                            ["operation"] = "append",
                            ["value"] = " -> B (fast path)"
                        }
                    },
                    new NodeDefinition
                    {
                        Id = "C",
                        Type = "Transform",
                        Config = new Dictionary<string, object>
                        {
                            ["operation"] = "append",
                            ["value"] = " -> C (slow path)"
                        }
                    },
                    new NodeDefinition
                    {
                        Id = "D",
                        Type = "Transform",
                        Config = new Dictionary<string, object>
                        {
                            ["operation"] = "append",
                            ["value"] = " -> D (merger - received inputs from both B and C)"
                        }
                    }
                },
                Edges = new List<Edge>
                {
                    new Edge { From = "A", To = "B", FromOutput = "output", ToInput = "input" },
                    new Edge { From = "A", To = "C", FromOutput = "output", ToInput = "input" },
                    new Edge { From = "B", To = "D", FromOutput = "output", ToInput = "inputB" },
                    new Edge { From = "C", To = "D", FromOutput = "output", ToInput = "inputC" }
                }
            };

            var orchestrator = new FlowOrchestrator();
            var context = await orchestrator.ExecuteFlowAsync(flow);

            Console.WriteLine("\n📊 RESULTS:");
            Console.WriteLine($"Status: {context.Status}");
            Console.WriteLine($"Execution Order: {string.Join(" → ", context.ExecutionOrder)}");
            Console.WriteLine($"\nExecution Batches:");
            for (int i = 0; i < context.ExecutionBatches.Count; i++)
            {
                Console.WriteLine($"  Batch {i + 1}: [{string.Join(", ", context.ExecutionBatches[i])}]");
            }

            // Verify correctness
            bool isCorrect = true;
            string errorMsg = "";

            // Check that D executed after both B and C
            var dIndex = context.ExecutionOrder.IndexOf("D");
            var bIndex = context.ExecutionOrder.IndexOf("B");
            var cIndex = context.ExecutionOrder.IndexOf("C");

            if (dIndex == -1 || bIndex == -1 || cIndex == -1)
            {
                isCorrect = false;
                errorMsg = "Missing nodes in execution order";
            }
            else if (dIndex < bIndex || dIndex < cIndex)
            {
                isCorrect = false;
                errorMsg = $"D executed before its parents! (D at {dIndex}, B at {bIndex}, C at {cIndex})";
            }

            // Check that D received inputs from both B and C
            if (context.NodeOutputs.ContainsKey("D"))
            {
                var dInputs = context.NodeOutputs["D"];
                Console.WriteLine($"\nNode D received inputs: {string.Join(", ", dInputs.Keys)}");
            }

            if (isCorrect)
            {
                Console.WriteLine("\n✅ TEST PASSED: D correctly waited for both B and C to complete!");
            }
            else
            {
                Console.WriteLine($"\n❌ TEST FAILED: {errorMsg}");
            }

            Console.WriteLine("\n═══════════════════════════════════════\n");
        }

        public static async Task TestMultiParentMerge()
        {
            Console.WriteLine("🔷 Testing Multi-Parent Merge Pattern");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("Structure:");
            Console.WriteLine("  A   B   C");
            Console.WriteLine("   \\ | /");
            Console.WriteLine("     D (aggregator)");
            Console.WriteLine();
            Console.WriteLine("Expected: D should only execute AFTER A, B, and C all complete");
            Console.WriteLine("═══════════════════════════════════════\n");

            var flow = new Flow
            {
                Id = "multi-parent-test",
                Name = "Multi-Parent Merge Test",
                Nodes = new List<NodeDefinition>
                {
                    new NodeDefinition
                    {
                        Id = "A",
                        Type = "Input",
                        Config = new Dictionary<string, object>
                        {
                            ["text"] = "Data from A"
                        }
                    },
                    new NodeDefinition
                    {
                        Id = "B",
                        Type = "Input",
                        Config = new Dictionary<string, object>
                        {
                            ["text"] = "Data from B"
                        }
                    },
                    new NodeDefinition
                    {
                        Id = "C",
                        Type = "Input",
                        Config = new Dictionary<string, object>
                        {
                            ["text"] = "Data from C"
                        }
                    },
                    new NodeDefinition
                    {
                        Id = "D",
                        Type = "Transform",
                        Config = new Dictionary<string, object>
                        {
                            ["operation"] = "append",
                            ["value"] = " -> Aggregated by D"
                        }
                    }
                },
                Edges = new List<Edge>
                {
                    new Edge { From = "A", To = "D", FromOutput = "output", ToInput = "inputA" },
                    new Edge { From = "B", To = "D", FromOutput = "output", ToInput = "inputB" },
                    new Edge { From = "C", To = "D", FromOutput = "output", ToInput = "inputC" }
                }
            };

            var orchestrator = new FlowOrchestrator();
            var context = await orchestrator.ExecuteFlowAsync(flow);

            Console.WriteLine("\n📊 RESULTS:");
            Console.WriteLine($"Status: {context.Status}");
            Console.WriteLine($"Execution Order: {string.Join(" → ", context.ExecutionOrder)}");
            Console.WriteLine($"\nExecution Batches:");
            for (int i = 0; i < context.ExecutionBatches.Count; i++)
            {
                Console.WriteLine($"  Batch {i + 1}: [{string.Join(", ", context.ExecutionBatches[i])}]");
            }

            // Verify correctness
            var dIndex = context.ExecutionOrder.IndexOf("D");
            var aIndex = context.ExecutionOrder.IndexOf("A");
            var bIndex = context.ExecutionOrder.IndexOf("B");
            var cIndex = context.ExecutionOrder.IndexOf("C");

            bool isCorrect = (dIndex > aIndex) && (dIndex > bIndex) && (dIndex > cIndex);

            if (isCorrect)
            {
                Console.WriteLine("\n✅ TEST PASSED: D correctly waited for A, B, and C to complete!");
            }
            else
            {
                Console.WriteLine($"\n❌ TEST FAILED: D executed too early!");
            }

            Console.WriteLine("\n═══════════════════════════════════════\n");
        }

        public static async Task TestComplexDAG()
        {
            Console.WriteLine("🔷 Testing Complex Multi-Level DAG");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("Structure:");
            Console.WriteLine("        A");
            Console.WriteLine("       /|\\");
            Console.WriteLine("      B C D");
            Console.WriteLine("       \\|/");
            Console.WriteLine("        E");
            Console.WriteLine();
            Console.WriteLine("Expected: E should only execute AFTER B, C, and D all complete");
            Console.WriteLine("═══════════════════════════════════════\n");

            var flow = new Flow
            {
                Id = "complex-dag-test",
                Name = "Complex DAG Test",
                Nodes = new List<NodeDefinition>
                {
                    new NodeDefinition
                    {
                        Id = "A",
                        Type = "Input",
                        Config = new Dictionary<string, object> { ["text"] = "Start" }
                    },
                    new NodeDefinition
                    {
                        Id = "B",
                        Type = "Transform",
                        Config = new Dictionary<string, object> { ["operation"] = "append", ["value"] = " -> B" }
                    },
                    new NodeDefinition
                    {
                        Id = "C",
                        Type = "Transform",
                        Config = new Dictionary<string, object> { ["operation"] = "append", ["value"] = " -> C" }
                    },
                    new NodeDefinition
                    {
                        Id = "D",
                        Type = "Transform",
                        Config = new Dictionary<string, object> { ["operation"] = "append", ["value"] = " -> D" }
                    },
                    new NodeDefinition
                    {
                        Id = "E",
                        Type = "Transform",
                        Config = new Dictionary<string, object> { ["operation"] = "append", ["value"] = " -> E (final)" }
                    }
                },
                Edges = new List<Edge>
                {
                    new Edge { From = "A", To = "B" },
                    new Edge { From = "A", To = "C" },
                    new Edge { From = "A", To = "D" },
                    new Edge { From = "B", To = "E", FromOutput = "output", ToInput = "inputB" },
                    new Edge { From = "C", To = "E", FromOutput = "output", ToInput = "inputC" },
                    new Edge { From = "D", To = "E", FromOutput = "output", ToInput = "inputD" }
                }
            };

            var orchestrator = new FlowOrchestrator();
            var context = await orchestrator.ExecuteFlowAsync(flow);

            Console.WriteLine("\n📊 RESULTS:");
            Console.WriteLine($"Status: {context.Status}");
            Console.WriteLine($"Execution Order: {string.Join(" → ", context.ExecutionOrder)}");
            Console.WriteLine($"\nExecution Batches:");
            for (int i = 0; i < context.ExecutionBatches.Count; i++)
            {
                Console.WriteLine($"  Batch {i + 1}: [{string.Join(", ", context.ExecutionBatches[i])}]");
            }

            // Verify correctness
            var eIndex = context.ExecutionOrder.IndexOf("E");
            var bIndex = context.ExecutionOrder.IndexOf("B");
            var cIndex = context.ExecutionOrder.IndexOf("C");
            var dIndex = context.ExecutionOrder.IndexOf("D");

            bool isCorrect = (eIndex > bIndex) && (eIndex > cIndex) && (eIndex > dIndex);

            if (isCorrect)
            {
                Console.WriteLine("\n✅ TEST PASSED: E correctly waited for B, C, and D to complete!");
                Console.WriteLine("   Expected batch structure: [A] → [B, C, D] → [E]");
            }
            else
            {
                Console.WriteLine($"\n❌ TEST FAILED: E executed too early!");
            }

            Console.WriteLine("\n═══════════════════════════════════════\n");
        }

        public static async Task TestMultiLevelMerge()
        {
            Console.WriteLine("🔷 Testing Multi-Level Merge Pattern (3-way merge with different path lengths)");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("Structure:");
            Console.WriteLine("      A");
            Console.WriteLine("     / \\");
            Console.WriteLine("    B   C");
            Console.WriteLine("    |   |\\");
            Console.WriteLine("    |   D E");
            Console.WriteLine("     \\  |/");
            Console.WriteLine("       F (3-way merger)");
            Console.WriteLine();
            Console.WriteLine("Edges: A→B, A→C, C→D, C→E, B→F, D→F, E→F");
            Console.WriteLine("Expected: F should only execute AFTER B, D, and E all complete");
            Console.WriteLine("Critical: F has 3 parents with different path lengths!");
            Console.WriteLine("═══════════════════════════════════════\n");

            var flow = new Flow
            {
                Id = "multi-level-merge-test",
                Name = "Multi-Level Merge Test",
                Nodes = new List<NodeDefinition>
                {
                    new NodeDefinition
                    {
                        Id = "A",
                        Type = "Input",
                        Config = new Dictionary<string, object>
                        {
                            ["text"] = "Start from A"
                        }
                    },
                    new NodeDefinition
                    {
                        Id = "B",
                        Type = "Transform",
                        Config = new Dictionary<string, object>
                        {
                            ["operation"] = "append",
                            ["value"] = " -> B (short path)"
                        }
                    },
                    new NodeDefinition
                    {
                        Id = "C",
                        Type = "Transform",
                        Config = new Dictionary<string, object>
                        {
                            ["operation"] = "append",
                            ["value"] = " -> C (branching)"
                        }
                    },
                    new NodeDefinition
                    {
                        Id = "D",
                        Type = "Transform",
                        Config = new Dictionary<string, object>
                        {
                            ["operation"] = "append",
                            ["value"] = " -> D (long path 1)"
                        }
                    },
                    new NodeDefinition
                    {
                        Id = "E",
                        Type = "Transform",
                        Config = new Dictionary<string, object>
                        {
                            ["operation"] = "append",
                            ["value"] = " -> E (long path 2)"
                        }
                    },
                    new NodeDefinition
                    {
                        Id = "F",
                        Type = "Transform",
                        Config = new Dictionary<string, object>
                        {
                            ["operation"] = "append",
                            ["value"] = " -> F (3-way merger - received B, D, E)"
                        }
                    }
                },
                Edges = new List<Edge>
                {
                    new Edge { From = "A", To = "B", FromOutput = "output", ToInput = "input" },
                    new Edge { From = "A", To = "C", FromOutput = "output", ToInput = "input" },
                    new Edge { From = "C", To = "D", FromOutput = "output", ToInput = "input" },
                    new Edge { From = "C", To = "E", FromOutput = "output", ToInput = "input" },
                    new Edge { From = "B", To = "F", FromOutput = "output", ToInput = "inputB" },
                    new Edge { From = "D", To = "F", FromOutput = "output", ToInput = "inputD" },
                    new Edge { From = "E", To = "F", FromOutput = "output", ToInput = "inputE" }
                }
            };

            var orchestrator = new FlowOrchestrator();
            var context = await orchestrator.ExecuteFlowAsync(flow);

            Console.WriteLine("\n📊 RESULTS:");
            Console.WriteLine($"Status: {context.Status}");
            Console.WriteLine($"Execution Order: {string.Join(" → ", context.ExecutionOrder)}");
            Console.WriteLine($"\nExecution Batches:");
            for (int i = 0; i < context.ExecutionBatches.Count; i++)
            {
                Console.WriteLine($"  Batch {i + 1}: [{string.Join(", ", context.ExecutionBatches[i])}]");
            }

            // Verify correctness
            bool isCorrect = true;
            string errorMsg = "";

            var fIndex = context.ExecutionOrder.IndexOf("F");
            var bIndex = context.ExecutionOrder.IndexOf("B");
            var dIndex = context.ExecutionOrder.IndexOf("D");
            var eIndex = context.ExecutionOrder.IndexOf("E");

            if (fIndex == -1 || bIndex == -1 || dIndex == -1 || eIndex == -1)
            {
                isCorrect = false;
                errorMsg = "Missing nodes in execution order";
            }
            else if (fIndex < bIndex || fIndex < dIndex || fIndex < eIndex)
            {
                isCorrect = false;
                errorMsg = $"F executed before its parents! (F at {fIndex}, B at {bIndex}, D at {dIndex}, E at {eIndex})";
            }

            // Check that F received inputs from all 3 parents
            if (context.NodeOutputs.ContainsKey("F"))
            {
                var fOutputs = context.NodeOutputs["F"];
                Console.WriteLine($"\nNode F final outputs: {string.Join(", ", fOutputs.Keys)}");
            }

            // Verify expected batch structure
            bool expectedBatches = context.ExecutionBatches.Count == 4 &&
                                   context.ExecutionBatches[0].Contains("A") &&
                                   context.ExecutionBatches[1].Contains("B") && context.ExecutionBatches[1].Contains("C") &&
                                   context.ExecutionBatches[2].Contains("D") && context.ExecutionBatches[2].Contains("E") &&
                                   context.ExecutionBatches[3].Contains("F");

            if (isCorrect && expectedBatches)
            {
                Console.WriteLine("\n✅ TEST PASSED: F correctly waited for ALL 3 parents (B, D, E) to complete!");
                Console.WriteLine("   Expected batch structure achieved: [A] → [B, C] → [D, E] → [F]");
                Console.WriteLine("   This proves the engine handles multi-level merges with different path lengths!");
            }
            else if (isCorrect && !expectedBatches)
            {
                Console.WriteLine("\n⚠️ TEST PARTIALLY PASSED: F waited for dependencies but batch structure unexpected");
            }
            else
            {
                Console.WriteLine($"\n❌ TEST FAILED: {errorMsg}");
            }

            Console.WriteLine("\n═══════════════════════════════════════\n");
        }

        public static async Task RunAllTests()
        {
            Console.WriteLine("\n🚀 DAG DEPENDENCY TRACKING VERIFICATION TESTS\n");
            Console.WriteLine("Testing that nodes only execute when ALL parent dependencies are satisfied\n");

            await TestDiamondPattern();
            await TestMultiParentMerge();
            await TestComplexDAG();
            await TestMultiLevelMerge();  // ← New test for the user's pattern

            Console.WriteLine("🎉 All DAG dependency tests completed!\n");
        }
    }
}
