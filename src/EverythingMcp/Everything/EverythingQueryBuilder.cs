namespace EverythingMcp.Everything;

public static class EverythingQueryBuilder
{
    public static string Build(EverythingSearchOptions options)
    {
        var folder = NormalizeFolder(options.Folder);
        var hasName = !string.IsNullOrWhiteSpace(options.Name);
        var hasExtension = !string.IsNullOrWhiteSpace(options.Extension);
        var hasQuery = !string.IsNullOrWhiteSpace(options.Query);
        var hasPathContains = !string.IsNullOrWhiteSpace(options.PathContains);
        var hasFolder = folder is not null;

        if (!hasName && !hasExtension && !hasQuery && !hasPathContains && !hasFolder)
        {
            throw new ArgumentException(
                "Provide at least one of: name, extension, folder, path_contains, or query.");
        }

        var parts = new List<string>();

        if (hasFolder)
        {
            parts.Add($"\"{folder}\"");
        }

        if (string.Equals(options.Kind, "folder", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add("folder:");
        }

        if (hasExtension)
        {
            parts.Add($"ext:{options.Extension!.Trim().TrimStart('.')}");
        }

        if (hasName)
        {
            parts.Add(EscapeQueryToken(options.Name!.Trim()));
        }
        else if (hasFolder && !hasExtension && !hasQuery && !hasPathContains)
        {
            // Browse a folder without learning Everything syntax.
            parts.Add("*");
        }

        if (hasPathContains)
        {
            parts.Add(EscapeQueryToken(options.PathContains!.Trim()));
        }

        if (hasQuery)
        {
            parts.Add(options.Query!.Trim());
        }

        return string.Join(' ', parts);
    }

    public static string? NormalizeFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return null;
        }

        return folder.Trim().TrimEnd('\\', '/');
    }

    private static string EscapeQueryToken(string value)
    {
        return value.Contains(' ') ? $"\"{value}\"" : value;
    }
}
