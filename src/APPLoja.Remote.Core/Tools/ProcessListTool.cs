using System.Diagnostics;
using System.Text.Json;

namespace APPLoja.Remote.Core.Tools;

public sealed class ProcessListTool : IRemoteTool
{
    public string Name => "system.processes";

    public Task<ToolExecutionResult> ExecuteAsync(JsonElement? parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var processes = Process.GetProcesses()
            .Select(process =>
            {
                try
                {
                    return new ProcessSnapshot(process.Id, process.ProcessName);
                }
                catch
                {
                    return new ProcessSnapshot(process.Id, "<unavailable>");
                }
                finally
                {
                    process.Dispose();
                }
            })
            .OrderBy(process => process.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(process => process.Id)
            .ToArray();

        return Task.FromResult(ToolExecutionResult.Ok(processes));
    }
}

public sealed record ProcessSnapshot(int Id, string Name);
