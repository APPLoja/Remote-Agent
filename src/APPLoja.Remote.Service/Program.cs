using APPLoja.Remote.Core.Tools;
using APPLoja.Remote.Service;
using APPLoja.Remote.Service.Configuration;
using APPLoja.Remote.Service.Transport;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "APPLoja Remote Agent";
});

builder.Services.Configure<AgentOptions>(
    builder.Configuration.GetSection(AgentOptions.SectionName));

builder.Services.AddSingleton<IRemoteTool, SystemInfoTool>();
builder.Services.AddSingleton<IRemoteTool, ProcessListTool>();
builder.Services.AddSingleton<IRemoteTool, InstalledSoftwareTool>();
builder.Services.AddSingleton<ToolRegistry>();
builder.Services.AddSingleton<RemoteAgentWebSocketClient>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
