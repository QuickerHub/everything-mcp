using System.Diagnostics;

namespace EverythingMcp.Everything;

public static class EverythingProcess
{
    private static readonly string[] InstallCandidates =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything", "Everything.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Everything", "Everything.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Everything", "Everything.exe"),
    ];

    public static bool IsRunning()
    {
        return Process.GetProcessesByName("Everything").Length > 0
            || Process.GetProcessesByName("Everything64").Length > 0;
    }

    public static string? ResolveInstallPath()
    {
        foreach (var candidate in InstallCandidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    public static void EnsureRunning(int waitMilliseconds = 4000)
    {
        if (IsRunning())
        {
            return;
        }

        var installPath = ResolveInstallPath()
            ?? throw new EverythingIpcException();

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = installPath,
            Arguments = "-startup",
            UseShellExecute = true,
        });

        if (process is null)
        {
            throw new EverythingException($"Failed to start Everything from {installPath}");
        }

        var deadline = Environment.TickCount64 + waitMilliseconds;
        while (Environment.TickCount64 < deadline)
        {
            if (IsRunning())
            {
                Thread.Sleep(500);
                return;
            }

            Thread.Sleep(250);
        }

        if (!IsRunning())
        {
            throw new EverythingIpcException();
        }
    }
}
