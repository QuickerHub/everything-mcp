using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using EverythingMcp.Everything;
using ModelContextProtocol.Server;

namespace EverythingMcp.Mcp;

[McpServerToolType]
public sealed class EverythingMcpTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    [McpServerTool(Name = "search")]
    [Description(
        "Search files and folders using voidtools Everything index. "
        + "Ideal for cross-project path discovery on Windows.")]
    public Task<string> SearchAsync(
        [Description("Everything search query. Supports wildcards, ext:, path:, folder:, etc.")]
        string query,
        [Description("Maximum number of results (default 100, max 1000).")]
        int? max_results = null,
        [Description("Limit search to this folder and its subfolders.")]
        string? scope_path = null,
        [Description("Match against full path instead of filename only.")]
        bool? match_path = null,
        [Description("Case-sensitive search.")]
        bool? match_case = null,
        [Description("Match whole words only.")]
        bool? match_whole_word = null,
        [Description("Enable regex search.")]
        bool? regex = null,
        [Description("Sort order: name_asc, name_desc, path_asc, path_desc, size_asc, size_desc, date_modified_asc, date_modified_desc.")]
        string? sort_by = null,
        [Description("Try to start Everything tray client if it is not running.")]
        bool auto_start = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("query must not be empty.", nameof(query));
        }

        if (auto_start)
        {
            EverythingProcess.EnsureRunning();
        }
        else if (!EverythingProcess.IsRunning())
        {
            throw new EverythingIpcException();
        }

        var searchQuery = BuildSearchQuery(query.Trim(), scope_path);
        var maxCount = Math.Clamp(max_results ?? 100, 1, 1000);

        using var api = new EverythingApi
        {
            MatchPath = match_path ?? false,
            MatchCase = match_case ?? false,
            MatchWholeWord = match_whole_word ?? false,
            EnableRegex = regex ?? false,
            Sort = MapSort(sort_by),
        };

        var results = api.Search(searchQuery, maxCount: (uint)maxCount, cancellationToken: cancellationToken);
        var payload = new
        {
            query = searchQuery,
            count = results.Count,
            results = results.Select(item => new
            {
                path = item.FilePath,
                name = item.FileName,
                size = item.Size,
                modified = item.Modified == DateTime.MinValue ? null : item.Modified.ToString("O"),
                is_folder = item.IsFolder,
            }),
        };

        return Task.FromResult(JsonSerializer.Serialize(payload, JsonOptions));
    }

    [McpServerTool(Name = "status")]
    [Description("Check bundled Everything SDK DLL and installed Everything client availability.")]
    public Task<string> StatusAsync()
    {
        var dllPath = Path.Combine(AppContext.BaseDirectory, "Everything64.dll");
        var payload = new
        {
            platform = OperatingSystem.IsWindows() ? "windows" : "unsupported",
            sdk_dll = new
            {
                path = dllPath,
                exists = File.Exists(dllPath),
            },
            everything = new
            {
                running = EverythingProcess.IsRunning(),
                install_path = EverythingProcess.ResolveInstallPath(),
            },
        };

        return Task.FromResult(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static string BuildSearchQuery(string query, string? scopePath)
    {
        if (string.IsNullOrWhiteSpace(scopePath))
        {
            return query;
        }

        var normalizedScope = scopePath.Trim().TrimEnd('\\', '/');
        return $"\"{normalizedScope}\" {query}";
    }

    private static uint MapSort(string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "name_asc" => EverythingApi.EverythingSort.NameAscending,
            "name_desc" => EverythingApi.EverythingSort.NameDescending,
            "path_asc" => EverythingApi.EverythingSort.PathAscending,
            "path_desc" => EverythingApi.EverythingSort.PathDescending,
            "size_asc" => EverythingApi.EverythingSort.SizeAscending,
            "size_desc" => EverythingApi.EverythingSort.SizeDescending,
            "date_modified_asc" => EverythingApi.EverythingSort.DateModifiedAscending,
            "date_modified_desc" or null or "" => EverythingApi.EverythingSort.DateModifiedDescending,
            _ => throw new ArgumentException(
                $"Unsupported sort_by value: {sortBy}.",
                nameof(sortBy)),
        };
    }
}
