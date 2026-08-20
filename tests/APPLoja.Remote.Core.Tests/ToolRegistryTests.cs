using APPLoja.Remote.Core.Protocol;
using APPLoja.Remote.Core.Tools;
using Xunit;

namespace APPLoja.Remote.Core.Tests;

public sealed class ToolRegistryTests
{
    [Fact]
    public async Task UnknownActionReturnsFailure()
    {
        var registry = new ToolRegistry([new SystemInfoTool()]);
        var job = new RemoteJob("job-1", "unknown.action");

        var result = await registry.ExecuteAsync(job, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Unknown action", result.Error);
    }

    [Fact]
    public async Task JobRequiringApprovalIsBlocked()
    {
        var registry = new ToolRegistry([new SystemInfoTool()]);
        var job = new RemoteJob("job-2", "system.info", RequiresApproval: true);

        var result = await registry.ExecuteAsync(job, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("requires approval", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SystemInfoReturnsMachineSnapshot()
    {
        var registry = new ToolRegistry([new SystemInfoTool()]);
        var job = new RemoteJob("job-3", "system.info");

        var result = await registry.ExecuteAsync(job, CancellationToken.None);

        Assert.True(result.Success);
        var snapshot = Assert.IsType<SystemInfoSnapshot>(result.Data);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.MachineName));
        Assert.True(snapshot.ProcessorCount > 0);
    }
}
