using System.Diagnostics;

namespace EverythingMcp.Everything;

public static class EverythingInstallGuide
{
    public const string WingetPackageId = "voidtools.Everything";
    public const string DownloadUrl = "https://www.voidtools.com/downloads/";

    public static bool IsWingetAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            });

            return process?.WaitForExit(5000) == true && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static object Build(bool sdkDllExists, bool installed, bool running)
    {
        var ready = sdkDllExists && installed && running;
        var issues = new List<string>();
        if (!sdkDllExists)
        {
            issues.Add("bundled_sdk_dll_missing");
        }

        if (!installed)
        {
            issues.Add("everything_not_installed");
        }
        else if (!running)
        {
            issues.Add("everything_not_running");
        }

        var wingetAvailable = IsWingetAvailable();
        var installMethods = new List<object>
        {
            new
            {
                method = "winget",
                available = wingetAvailable,
                command = $"winget install --id {WingetPackageId} -e",
                description = "Recommended on Windows 10/11. May prompt for elevation or user confirmation.",
            },
            new
            {
                method = "manual",
                available = true,
                url = DownloadUrl,
                description = "Download Everything 1.4.x (stable) installer from voidtools and run it.",
            },
        };

        return new
        {
            ready,
            issues,
            install_guide = ready
                ? null
                : new
                {
                    summary = "Everything.exe is required for file indexing. This MCP only bundles the SDK IPC client (Everything64.dll).",
                    recommended_method = wingetAvailable ? "winget" : "manual",
                    methods = installMethods,
                    steps = new[]
                    {
                        "Install Everything 1.4.x stable (avoid 1.5 alpha unless you intentionally need it).",
                        "Launch Everything once so it stays in the system tray.",
                        "Wait for the initial index to finish on first run.",
                        "Call status again, or run search with auto_start=true.",
                    },
                    after_install = new
                    {
                        start_command = installed
                            ? EverythingProcess.ResolveInstallPath() is { } path
                                ? $"\"{path}\" -startup"
                                : null
                            : null,
                        auto_start_note = "search uses auto_start=true by default and will try -startup when Everything is installed but not running.",
                    },
                    agent_note = "Tell the user to run the winget command in an elevated or interactive terminal, or install manually from voidtools. Do not assume silent install succeeds without user approval.",
                },
        };
    }
}
