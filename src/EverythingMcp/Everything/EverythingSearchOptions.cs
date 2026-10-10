namespace EverythingMcp.Everything;

public sealed class EverythingSearchOptions
{
    public string? Name { get; init; }
    public string? Extension { get; init; }
    public string? Folder { get; init; }
    public string? PathContains { get; init; }
    public string? Query { get; init; }
    public string Kind { get; init; } = "file";
    public bool ApplyDefaultFilters { get; init; } = true;
    public bool IncludeBuildArtifacts { get; init; }
    public bool IncludeHidden { get; init; }
    public IReadOnlyList<string> ExtraExclude { get; init; } = [];
}
