using FlowManager.Tests.Manual;

/// <summary>
/// Simple entry point to run DAG dependency tests
/// Run this to verify that the DAG execution engine correctly handles all dependency scenarios
/// </summary>
class RunDAGTests
{
    static async Task Main(string[] args)
    {
        await DAGDependencyTest.RunAllTests();
    }
}
