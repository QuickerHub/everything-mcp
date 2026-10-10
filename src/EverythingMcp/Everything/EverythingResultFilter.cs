namespace EverythingMcp.Everything;

public static class EverythingResultFilter
{
    private static readonly HashSet<string> DefaultNoiseSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules",
        ".git",
        "__pycache__",
        ".next",
        ".nuxt",
        ".turbo",
        ".cache",
        ".pnpm-store",
        "bower_components",
        ".venv",
        "venv",
        ".tox",
        ".pytest_cache",
        "TestResults",
        "vendor",
    };

    private static readonly HashSet<string> DefaultHiddenSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".vs",
        ".nuget",
        ".next",
        ".nuxt",
        ".cache",
        ".turbo",
        ".yarn",
        ".idea",
        ".gradle",
        ".pytest_cache",
    };

    private static readonly HashSet<string> BuildArtifactSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin",
        "obj",
        "dist",
        "build",
        "target",
        "artifacts",
        "out",
        "Debug",
        "Release",
        "node_modules",
    };

    public static IReadOnlyList<EverythingFileInfo> Apply(
        IReadOnlyList<EverythingFileInfo> results,
        EverythingSearchOptions options,
        int maxResults)
    {
        var filtered = new List<EverythingFileInfo>(Math.Min(results.Count, maxResults));

        foreach (var item in results)
        {
            if (filtered.Count >= maxResults)
            {
                break;
            }

            if (!Matches(item, options))
            {
                continue;
            }

            filtered.Add(item);
        }

        return filtered;
    }

    public static bool Matches(EverythingFileInfo item, EverythingSearchOptions options)
    {
        if (!MatchesKind(item, options.Kind))
        {
            return false;
        }

        return !ShouldExclude(item.FilePath, options);
    }

    private static bool MatchesKind(EverythingFileInfo item, string kind)
    {
        return kind.Trim().ToLowerInvariant() switch
        {
            "file" => !item.IsFolder,
            "folder" => item.IsFolder,
            "any" => true,
            _ => throw new ArgumentException($"Unsupported kind: {kind}. Use file, folder, or any."),
        };
    }

    private static bool ShouldExclude(string filePath, EverythingSearchOptions options)
    {
        var segments = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        foreach (var segment in segments)
        {
            if (options.ExtraExclude.Any(exclude =>
                    segment.Equals(exclude, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (!options.IncludeBuildArtifacts
                && BuildArtifactSegments.Contains(segment))
            {
                return true;
            }

            if (options.ApplyDefaultFilters && DefaultNoiseSegments.Contains(segment))
            {
                return true;
            }

            if (!options.IncludeHidden
                && segment.StartsWith('.')
                && segment.Length > 1
                && DefaultHiddenSegments.Contains(segment))
            {
                return true;
            }
        }

        return false;
    }
}
