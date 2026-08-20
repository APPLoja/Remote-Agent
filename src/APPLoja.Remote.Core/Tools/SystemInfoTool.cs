using System.Runtime.InteropServices;
using System.Text.Json;

namespace APPLoja.Remote.Core.Tools;

public sealed class SystemInfoTool : IRemoteTool
{
    public string Name => "system.info";

    public Task<ToolExecutionResult> ExecuteAsync(JsonElement? parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var drives = DriveInfo.GetDrives()
            .Where(drive => drive.IsReady)
            .Select(drive => new DriveSnapshot(
                drive.Name,
                drive.DriveFormat,
                drive.TotalSize,
                drive.AvailableFreeSpace))
            .ToArray();

        var snapshot = new SystemInfoSnapshot(
            Environment.MachineName,
            Environment.UserName,
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString(),
            RuntimeInformation.ProcessArchitecture.ToString(),
            Environment.OSVersion.VersionString,
            Environment.ProcessorCount,
            Environment.Is64BitOperatingSystem,
            drives,
            DateTimeOffset.UtcNow);

        return Task.FromResult(ToolExecutionResult.Ok(snapshot));
    }
}

public sealed record SystemInfoSnapshot(
    string MachineName,
    string UserName,
    string OsDescription,
    string OsArchitecture,
    string ProcessArchitecture,
    string OsVersion,
    int ProcessorCount,
    bool Is64BitOperatingSystem,
    IReadOnlyCollection<DriveSnapshot> Drives,
    DateTimeOffset CollectedAt);

public sealed record DriveSnapshot(
    string Name,
    string FileSystem,
    long TotalBytes,
    long AvailableBytes);
