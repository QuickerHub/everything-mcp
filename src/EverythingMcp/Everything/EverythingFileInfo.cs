namespace EverythingMcp.Everything;

public sealed class EverythingFileInfo
{
    public string FileName { get; init; } = string.Empty;

    public string FilePath { get; init; } = string.Empty;

    public DateTime Modified { get; init; }

    public long Size { get; init; }

    public bool IsFolder { get; init; }
}
