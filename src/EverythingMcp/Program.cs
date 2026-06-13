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

if (args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase))
{
    return RunSmokeTest();
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
