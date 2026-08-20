using APPLoja.Remote.Service.Configuration;
using APPLoja.Remote.Service.Transport;
using Microsoft.Extensions.Options;

namespace APPLoja.Remote.Service;

public sealed class Worker : BackgroundService
{
    private readonly AgentOptions _options;
    private readonly RemoteAgentWebSocketClient _client;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IOptions<AgentOptions> options,
        RemoteAgentWebSocketClient client,
        ILogger<Worker> logger)
    {
        _options = options.Value;
        _client = client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("APPLoja Remote Agent is installed but disabled. Set RemoteAgent:Enabled=true to connect.");
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            return;
        }

        await _client.RunAsync(stoppingToken);
    }
}
