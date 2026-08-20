namespace APPLoja.Remote.Service.Configuration;

public sealed class AgentOptions
{
    public const string SectionName = "RemoteAgent";

    public bool Enabled { get; init; }

    public string ServerUrl { get; init; } = string.Empty;

    public string DeviceId { get; init; } = string.Empty;

    public string DeviceToken { get; init; } = string.Empty;

    public int ReconnectDelaySeconds { get; init; } = 5;

    public int MaxMessageBytes { get; init; } = 1_048_576;
}
