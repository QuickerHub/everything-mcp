using EverythingMcp.Agent;
using EverythingMcp.Everything;
using EverythingMcp.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.Text.Json;

if (!OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("everything-mcp supports Windows only.");
    return 1;
}

if (args.Contains("--smoke-test-offline", StringComparer.OrdinalIgnoreCase))
{
    return RunOfflineSmokeTest();
}

if (args.Length >= 2
    && string.Equals(args[0], "agent", StringComparison.OrdinalIgnoreCase)
    && string.Equals(args[1], "setup", StringComparison.OrdinalIgnoreCase))
{
    var checkOnly = args.Any(arg => string.Equals(arg, "--check", StringComparison.OrdinalIgnoreCase));
    return EverythingAgentSetup.RunSetup(checkOnly);
}

if (args.Contains("--ping", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine(EverythingHealth.BuildPingJson());
    return 0;
}

if (args.Contains("--status", StringComparer.OrdinalIgnoreCase))
{
    return await RunStatusCliAsync();
}

if (args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase))
{
    return RunSmokeTest();
}

if (args.Contains("--search", StringComparer.OrdinalIgnoreCase))
{
    return await RunSearchCliAsync(args);
}

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(console =>
{
    console.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(EverythingMcpTools).Assembly);

await builder.Build().RunAsync();
return 0;

static int RunOfflineSmokeTest()
{
    var dllPath = Path.Combine(AppContext.BaseDirectory, "Everything64.dll");
    var ok = File.Exists(dllPath);
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        ok,
        mode = "offline",
        sdk_dll = dllPath,
        dotnet = Environment.Version.ToString(),
    }));
    return ok ? 0 : 1;
}

static async Task<int> RunStatusCliAsync()
{
    try
    {
        var tools = new EverythingMcpTools();
        Console.WriteLine(await tools.StatusAsync());
        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
        return 1;
    }
}

static async Task<int> RunSearchCliAsync(string[] args)
{
    try
    {
        var options = ParseCliOptions(args);
        var tools = new EverythingMcpTools();
        var json = await tools.SearchAsync(
            name: options.GetValueOrDefault("name"),
            extension: options.GetValueOrDefault("extension"),
            folder: options.GetValueOrDefault("folder"),
            path_contains: options.GetValueOrDefault("path_contains"),
            kind: options.GetValueOrDefault("kind") ?? "file",
            query: options.GetValueOrDefault("query"),
            max_results: options.TryGetValue("max_results", out var maxText)
                && int.TryParse(maxText, out var max)
                ? max
                : null);

        Console.WriteLine(json);
        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
        return 1;
    }
}

static Dictionary<string, string> ParseCliOptions(string[] args)
{
    var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var tokens = args.SkipWhile(arg => !string.Equals(arg, "--search", StringComparison.OrdinalIgnoreCase))
        .Skip(1)
        .ToArray();

    for (var index = 0; index < tokens.Length; index++)
    {
        var token = tokens[index];
        if (!token.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var key = token[2..];
        if (index + 1 >= tokens.Length)
        {
            break;
        }

        options[key] = tokens[++index];
    }

    return options;
}

static int RunSmokeTest()
{
    try
    {
        EverythingProcess.EnsureRunning();
        using var api = new EverythingApi { MatchPath = true };
        var results = api.Search("\"D:\\source\\repos\\quicker\" quicker-rpc", maxCount: 5);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            ok = true,
            count = results.Count,
            paths = results.Select(item => item.FilePath).ToArray(),
        }));
        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
        return 1;
    }
}
