using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using EverythingMcp.Agent;
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
        "Find files or folders on Windows using Everything. "
        + "Agent-friendly: prefer name, extension, and folder instead of raw Everything query syntax.")]
    public Task<string> SearchAsync(
        [Description("Filename to find. Supports wildcards (*, ?). Example: QuickerUtil.cs or *.cs")]
        string? name = null,
        [Description("File extension without dot. Example: cs, ts, md")]
        string? extension = null,
        [Description("Search only under this folder and subfolders. Example: D:\\source\\repos")]
        string? folder = null,
        [Description("Path must contain this text (filename or directory segment).")]
        string? path_contains = null,
        [Description("Result type: file (default), folder, or any.")]
        string kind = "file",
        [Description("Advanced raw Everything query. Use only when structured params are insufficient.")]
        string? query = null,
        [Description("Deprecated alias for folder.")]
        string? scope_path = null,
        [Description("Maximum results after filtering (default 25, max 100).")]
        int? max_results = null,
        [Description("Apply default noise filters (node_modules, .git, build outputs, etc.). Default true.")]
        bool apply_default_filters = true,
        [Description("Include bin/obj/dist/build/target/artifacts folders. Default false.")]
        bool include_build_artifacts = false,
        [Description("Include hidden dot-directories like .vs and .cache. Default false.")]
        bool include_hidden = false,
        [Description("Extra path segment names to exclude.")]
        string[]? exclude = null,
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
        if (auto_start)
        {
            EverythingProcess.EnsureRunning();
        }
        else if (!EverythingProcess.IsRunning())
        {
            throw new EverythingIpcException();
        }

        var resolvedFolder = EverythingQueryBuilder.NormalizeFolder(folder)
            ?? EverythingQueryBuilder.NormalizeFolder(scope_path);

        var options = new EverythingSearchOptions
        {
            Name = name,
            Extension = extension,
            Folder = resolvedFolder,
            PathContains = path_contains,
            Query = query,
            Kind = kind,
            ApplyDefaultFilters = apply_default_filters,
            IncludeBuildArtifacts = include_build_artifacts,
            IncludeHidden = include_hidden,
            ExtraExclude = exclude ?? [],
        };

        var searchQuery = EverythingQueryBuilder.Build(options);
        var maxCount = Math.Clamp(max_results ?? 25, 1, 100);
        var usePostFilter = apply_default_filters
            || !include_build_artifacts
            || !include_hidden
            || (exclude?.Length ?? 0) > 0
            || !string.Equals(kind, "any", StringComparison.OrdinalIgnoreCase);

        using var api = new EverythingApi
        {
            MatchPath = match_path ?? !string.IsNullOrWhiteSpace(path_contains),
            MatchCase = match_case ?? false,
            MatchWholeWord = match_whole_word ?? false,
            EnableRegex = regex ?? false,
            Sort = MapSort(sort_by),
        };

        List<EverythingFileInfo> results;
        int rawCount;

        if (!usePostFilter)
        {
            var rawResults = api.Search(searchQuery, maxCount: (uint)maxCount, cancellationToken: cancellationToken);
            rawCount = rawResults.Count;
            results = rawResults.ToList();
        }
        else
        {
            (results, rawCount) = SearchWithFiltering(
                api,
                searchQuery,
                options,
                maxCount,
                cancellationToken);
        }

        var payload = new
        {
            query = searchQuery,
            params_used = new
            {
                name,
                extension,
                folder = resolvedFolder,
                path_contains,
                kind,
                advanced_query = query,
            },
            filters = new
            {
                apply_default_filters,
                include_build_artifacts,
                include_hidden,
                exclude = exclude ?? Array.Empty<string>(),
            },
            raw_count = rawCount,
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
    [Description(
        "Check bundled Everything SDK DLL and installed Everything client availability. "
        + "When not ready, returns install_guide with winget/manual steps.")]
    public Task<string> StatusAsync()
    {
        var dllPath = Path.Combine(AppContext.BaseDirectory, "Everything64.dll");
        var sdkExists = File.Exists(dllPath);
        var installPath = EverythingProcess.ResolveInstallPath();
        var installed = installPath is not null;
        var running = EverythingProcess.IsRunning();
        var guide = EverythingInstallGuide.Build(sdkExists, installed, running);

        var payload = new
        {
            platform = OperatingSystem.IsWindows() ? "windows" : "unsupported",
            sdk_dll = new
            {
                path = dllPath,
                exists = sdkExists,
            },
            everything = new
            {
                installed,
                running,
                install_path = installPath,
            },
            guide,
            cli_fallback = EverythingAgentSetup.BuildCliFallback(
                EverythingAgentSetup.TryResolveExePathQuiet()),
            agent_note = EverythingAgentSetup.BuildAgentNote(),
        };

        return Task.FromResult(JsonSerializer.Serialize(payload, JsonOptions));
    }

    [McpServerTool(Name = "install_guide")]
    [Description(
        "Return structured Everything installation guidance for Windows. "
        + "Use when status shows everything_not_installed or the user asks how to install Everything.")]
    public Task<string> InstallGuideAsync()
    {
        var dllPath = Path.Combine(AppContext.BaseDirectory, "Everything64.dll");
        var sdkExists = File.Exists(dllPath);
        var installPath = EverythingProcess.ResolveInstallPath();
        var installed = installPath is not null;
        var running = EverythingProcess.IsRunning();
        var payload = EverythingInstallGuide.Build(sdkExists, installed, running);

        return Task.FromResult(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static (List<EverythingFileInfo> Results, int RawCount) SearchWithFiltering(
        EverythingApi api,
        string searchQuery,
        EverythingSearchOptions options,
        int maxCount,
        CancellationToken cancellationToken)
    {
        const int pageSize = 200;
        const int maxPages = 5;

        var accepted = new List<EverythingFileInfo>(maxCount);
        var rawCount = 0;
        uint offset = 0;

        for (var page = 0; page < maxPages && accepted.Count < maxCount; page++)
        {
            var batch = api.Search(
                searchQuery,
                offset: offset,
                maxCount: pageSize,
                cancellationToken: cancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            rawCount += batch.Count;

            foreach (var item in batch)
            {
                if (accepted.Count >= maxCount)
                {
                    break;
                }

                if (!EverythingResultFilter.Matches(item, options))
                {
                    continue;
                }

                accepted.Add(item);
            }

            if (batch.Count < pageSize)
            {
                break;
            }

            offset += pageSize;
        }

        return (accepted, rawCount);
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
