using System.Text.Json;

namespace APPLoja.Remote.Core.Protocol;

public sealed record RemoteJob(
    string Id,
    string Action,
    JsonElement? Parameters = null,
    DateTimeOffset? CreatedAt = null,
    bool RequiresApproval = false);

public sealed record RemoteJobResult(
    string JobId,
    string Action,
    bool Success,
    object? Data = null,
    string? Error = null,
    DateTimeOffset? CompletedAt = null)
{
    public static RemoteJobResult Succeeded(RemoteJob job, object? data) =>
        new(job.Id, job.Action, true, data, null, DateTimeOffset.UtcNow);

    public static RemoteJobResult Failed(RemoteJob job, string error) =>
        new(job.Id, job.Action, false, null, error, DateTimeOffset.UtcNow);
}

public sealed record AgentHello(
    string Type,
    string DeviceId,
    string MachineName,
    string AgentVersion,
    string Platform,
    DateTimeOffset SentAt);
