using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using APPLoja.Remote.Core.Protocol;
using APPLoja.Remote.Core.Tools;
using APPLoja.Remote.Service.Configuration;
using Microsoft.Extensions.Options;

namespace APPLoja.Remote.Service.Transport;

public sealed class RemoteAgentWebSocketClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AgentOptions _options;
    private readonly ToolRegistry _tools;
    private readonly ILogger<RemoteAgentWebSocketClient> _logger;

    public RemoteAgentWebSocketClient(
        IOptions<AgentOptions> options,
        ToolRegistry tools,
        ILogger<RemoteAgentWebSocketClient> logger)
    {
        _options = options.Value;
        _tools = tools;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunSessionAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Remote Agent connection ended unexpectedly.");
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(Math.Max(1, _options.ReconnectDelaySeconds)),
                    cancellationToken);
            }
        }
    }

    private async Task RunSessionAsync(CancellationToken cancellationToken)
    {
        using var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("Authorization", $"Bearer {_options.DeviceToken}");
        socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

        var uri = new Uri(_options.ServerUrl, UriKind.Absolute);
        _logger.LogInformation("Connecting Remote Agent device {DeviceId} to {ServerUrl}.", _options.DeviceId, uri);

        await socket.ConnectAsync(uri, cancellationToken);
        await SendHelloAsync(socket, cancellationToken);

        _logger.LogInformation("Remote Agent connected. Available tools: {Tools}.", string.Join(", ", _tools.ToolNames));

        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var payload = await ReceiveTextAsync(socket, cancellationToken);
            if (payload is null)
            {
                break;
            }

            RemoteJob? job;
            try
            {
                job = JsonSerializer.Deserialize<RemoteJob>(payload, JsonOptions);
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "Ignoring invalid job payload.");
                continue;
            }

            if (job is null || string.IsNullOrWhiteSpace(job.Id) || string.IsNullOrWhiteSpace(job.Action))
            {
                _logger.LogWarning("Ignoring job without id/action.");
                continue;
            }

            _logger.LogInformation("Executing job {JobId} action {Action}.", job.Id, job.Action);
            var result = await _tools.ExecuteAsync(job, cancellationToken);
            await SendJsonAsync(socket, result, cancellationToken);

            _logger.LogInformation(
                "Job {JobId} completed with success={Success}.",
                job.Id,
                result.Success);
        }
    }

    private async Task SendHelloAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "dev";
        var hello = new AgentHello(
            "agent.hello",
            _options.DeviceId,
            Environment.MachineName,
            version,
            RuntimeInformation.OSDescription,
            DateTimeOffset.UtcNow);

        await SendJsonAsync(socket, hello, cancellationToken);
    }

    private async Task<string?> ReceiveTextAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        using var stream = new MemoryStream();

        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                if (socket.State == WebSocketState.CloseReceived)
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "ack", cancellationToken);
                }

                return null;
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                throw new InvalidOperationException("Only text WebSocket messages are supported.");
            }

            stream.Write(buffer, 0, result.Count);
            if (stream.Length > _options.MaxMessageBytes)
            {
                throw new InvalidOperationException("Remote message exceeds the configured size limit.");
            }
        }
        while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static async Task SendJsonAsync(
        ClientWebSocket socket,
        object value,
        CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        await socket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            cancellationToken);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.ServerUrl) ||
            !Uri.TryCreate(_options.ServerUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeWss && uri.Scheme != Uri.UriSchemeWs))
        {
            throw new InvalidOperationException("RemoteAgent:ServerUrl must be a valid ws:// or wss:// URL.");
        }

        if (string.IsNullOrWhiteSpace(_options.DeviceId))
        {
            throw new InvalidOperationException("RemoteAgent:DeviceId is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.DeviceToken))
        {
            throw new InvalidOperationException("RemoteAgent:DeviceToken is required.");
        }
    }
}
