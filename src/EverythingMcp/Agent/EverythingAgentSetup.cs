using System.Text.Json;
using System.Text.Json.Nodes;

namespace EverythingMcp.Agent;

public static class EverythingAgentSetup
{
    public const string ServerName = "everything-search";

    public static string ResolveExePath()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EVERYTHING_MCP_EXE"))
            && File.Exists(Environment.GetEnvironmentVariable("EVERYTHING_MCP_EXE")!))
        {
            return Path.GetFullPath(Environment.GetEnvironmentVariable("EVERYTHING_MCP_EXE")!);
        }

        var sibling = Path.Combine(AppContext.BaseDirectory, "everything-mcp.exe");
        if (File.Exists(sibling))
        {
            return sibling;
        }

        throw new FileNotFoundException(
            "everything-mcp.exe not found. Run .\\build.ps1 -Publish or set EVERYTHING_MCP_EXE.");
    }

    public static string ResolveCursorMcpConfigPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cursor",
            "mcp.json");
    }

    public static string? TryResolveExePathQuiet()
    {
        try
        {
            return ResolveExePath();
        }
        catch
        {
            return null;
        }
    }

    public static bool RunCheckDotNetOnly() => HasDotNet8Runtime();

    public static int RunSetup(bool checkOnly)
    {
        var exePath = TryResolveExePathQuiet();
        var configPath = ResolveCursorMcpConfigPath();
        var issues = new List<object>();
        var nextSteps = new List<string>();

        if (exePath is null)
        {
            issues.Add(new
            {
                code = "exe_missing",
                message = "everything-mcp.exe not found",
                remediation = "Run .\\build.ps1 -Publish in everything-mcp repo",
            });
            nextSteps.Add("cd everything-mcp && .\\build.ps1 -Publish");
        }

        if (!OperatingSystem.IsWindows())
        {
            issues.Add(new
            {
                code = "platform_unsupported",
                message = "Everything MCP supports Windows only",
            });
        }

        var dotnetOk = HasDotNet8Runtime();
        if (!dotnetOk)
        {
            issues.Add(new
            {
                code = "dotnet8_missing",
                message = ".NET 8 runtime is required",
                remediation = "Install from https://dotnet.microsoft.com/download/dotnet/8.0",
            });
            nextSteps.Add("winget install Microsoft.DotNet.Runtime.8");
        }

        var mcpConfigured = false;
        string? configuredCommand = null;
        if (File.Exists(configPath))
        {
            try
            {
                var root = JsonNode.Parse(File.ReadAllText(configPath)) as JsonObject;
                var server = root?["mcpServers"]?[ServerName] as JsonObject;
                mcpConfigured = server is not null;
                configuredCommand = server?["command"]?.GetValue<string>();
            }
            catch
            {
                issues.Add(new
                {
                    code = "mcp_json_invalid",
                    message = $"Could not parse {configPath}",
                });
            }
        }
        else if (!checkOnly)
        {
            issues.Add(new
            {
                code = "mcp_json_missing",
                message = "Cursor MCP config does not exist yet",
            });
        }

        if (!checkOnly && exePath is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            MergeCursorMcpConfig(configPath, exePath);
            mcpConfigured = true;
            configuredCommand = exePath;
            nextSteps.Add("Reload MCP servers in Cursor (Settings → MCP → everything-search)");
        }
        else if (checkOnly && !mcpConfigured)
        {
            issues.Add(new
            {
                code = "mcp_not_configured",
                message = $"MCP server '{ServerName}' is missing from {configPath}",
                remediation = "everything-mcp.exe agent setup",
            });
            nextSteps.Add("everything-mcp.exe agent setup");
        }

        var ok = checkOnly
            ? issues.Count == 0 && mcpConfigured && exePath is not null
            : exePath is not null;

        var payload = new
        {
            ok,
            mode = checkOnly ? "check" : "setup",
            exe_path = exePath,
            mcp_config_path = configPath,
            mcp_configured = mcpConfigured,
            mcp_command = configuredCommand,
            dotnet8 = dotnetOk,
            cli_fallback = BuildCliFallback(exePath),
            agent_note = BuildAgentNote(),
            issues,
            next_steps = nextSteps.Distinct().ToArray(),
        };

        Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        return ok ? 0 : 1;
    }

    public static object BuildCliFallback(string? exePath)
    {
        var command = exePath ?? "everything-mcp.exe";
        return new
        {
            when = "MCP tool returns Not connected, or before first MCP call",
            search = $"{command} --search --name <name> --folder <path> [--kind file|folder|any]",
            status = $"{command} --status",
            ping = $"{command} --ping",
            example = $"{command} --search --name CeaQuickerTools --folder D:\\source\\repos --kind any",
        };
    }

    public static string BuildAgentNote()
    {
        return "Agents cannot restart Cursor MCP. Prefer MCP when connected; on Not connected, run CLI via shell and ask user to reload MCP only once.";
    }

    private static bool HasDotNet8Runtime()
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--list-runtimes",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null)
            {
                return false;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);
            return output.Contains("Microsoft.NETCore.App 8.", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static void MergeCursorMcpConfig(string configPath, string exePath)
    {
        JsonObject root;
        if (File.Exists(configPath))
        {
            root = JsonNode.Parse(File.ReadAllText(configPath)) as JsonObject ?? new JsonObject();
        }
        else
        {
            root = new JsonObject();
        }

        var servers = root["mcpServers"] as JsonObject ?? new JsonObject();
        servers[ServerName] = new JsonObject
        {
            ["command"] = exePath,
            ["args"] = new JsonArray(),
        };
        root["mcpServers"] = servers;

        File.WriteAllText(configPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}
