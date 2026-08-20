using System.Text.Json;

namespace APPLoja.Remote.Core.Tools;

public interface IRemoteTool
{
    string Name { get; }

    Task<ToolExecutionResult> ExecuteAsync(
        JsonElement? parameters,
        CancellationToken cancellationToken);
}

public sealed record ToolExecutionResult(bool Success, object? Data = null, string? Error = null)
{
    public static ToolExecutionResult Ok(object? data = null) => new(true, data);

    public static ToolExecutionResult Fail(string error) => new(false, null, error);
}
