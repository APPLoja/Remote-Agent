using System.Text.Json;
using Microsoft.Win32;

namespace APPLoja.Remote.Core.Tools;

public sealed class InstalledSoftwareTool : IRemoteTool
{
    private static readonly string[] RegistryPaths =
    [
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    ];

    public string Name => "software.installed";

    public Task<ToolExecutionResult> ExecuteAsync(JsonElement? parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult(ToolExecutionResult.Fail("software.installed is only supported on Windows."));
        }

        var software = new Dictionary<string, InstalledSoftwareSnapshot>(StringComparer.OrdinalIgnoreCase);

        ReadHive(Registry.LocalMachine, software, cancellationToken);
        ReadHive(Registry.CurrentUser, software, cancellationToken);

        var result = software.Values
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Version, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult(ToolExecutionResult.Ok(result));
    }

    private static void ReadHive(
        RegistryKey hive,
        IDictionary<string, InstalledSoftwareSnapshot> software,
        CancellationToken cancellationToken)
    {
        foreach (var path in RegistryPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var uninstall = hive.OpenSubKey(path);
            if (uninstall is null)
            {
                continue;
            }

            foreach (var subKeyName in uninstall.GetSubKeyNames())
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var entry = uninstall.OpenSubKey(subKeyName);
                var name = entry?.GetValue("DisplayName") as string;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var version = entry?.GetValue("DisplayVersion") as string;
                var publisher = entry?.GetValue("Publisher") as string;
                var installLocation = entry?.GetValue("InstallLocation") as string;
                var key = $"{name}|{version}|{publisher}";

                software[key] = new InstalledSoftwareSnapshot(
                    name,
                    version,
                    publisher,
                    installLocation);
            }
        }
    }
}

public sealed record InstalledSoftwareSnapshot(
    string Name,
    string? Version,
    string? Publisher,
    string? InstallLocation);
