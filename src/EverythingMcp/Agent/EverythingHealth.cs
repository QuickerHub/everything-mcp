using System.Text.Json;
using EverythingMcp.Everything;

namespace EverythingMcp.Agent;

public static class EverythingHealth
{
    public static string BuildPingJson()
    {
        var dllPath = Path.Combine(AppContext.BaseDirectory, "Everything64.dll");
        var sdkExists = File.Exists(dllPath);
        var installPath = EverythingProcess.ResolveInstallPath();
        var installed = installPath is not null;
        var running = EverythingProcess.IsRunning();
        var dotnetOk = EverythingAgentSetup.RunCheckDotNetOnly();

        var ready = sdkExists && installed && running && dotnetOk;
        var payload = new
        {
            ok = ready,
            mode = "ping",
            platform = OperatingSystem.IsWindows() ? "windows" : "unsupported",
            dotnet8 = dotnetOk,
            sdk_dll = sdkExists,
            everything_installed = installed,
            everything_running = running,
            install_path = installPath,
            cli_fallback = EverythingAgentSetup.BuildCliFallback(
                EverythingAgentSetup.TryResolveExePathQuiet()),
            agent_note = EverythingAgentSetup.BuildAgentNote(),
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }
}
