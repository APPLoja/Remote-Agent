using APPLoja.Remote.Core.Protocol;

namespace APPLoja.Remote.Core.Tools;

public sealed class ToolRegistry
{
    private readonly IReadOnlyDictionary<string, IRemoteTool> _tools;

    public ToolRegistry(IEnumerable<IRemoteTool> tools)
    {
        _tools = tools.ToDictionary(tool => tool.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> ToolNames => _tools.Keys.ToArray();

    public async Task<RemoteJobResult> ExecuteAsync(RemoteJob job, CancellationToken cancellationToken)
    {
        if (job.RequiresApproval)
        {
            return RemoteJobResult.Failed(job, "This job requires approval and cannot run automatically.");
        }

        if (!_tools.TryGetValue(job.Action, out var tool))
        {
            return RemoteJobResult.Failed(job, $"Unknown action: {job.Action}");
        }

        try
        {
            var result = await tool.ExecuteAsync(job.Parameters, cancellationToken);
            return result.Success
                ? RemoteJobResult.Succeeded(job, result.Data)
                : RemoteJobResult.Failed(job, result.Error ?? "Tool execution failed.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return RemoteJobResult.Failed(job, exception.Message);
        }
    }
}
