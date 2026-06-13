using System.Runtime.InteropServices;
using System.Text;

namespace EverythingMcp.Everything;

/// <summary>
/// P/Invoke wrapper for voidtools Everything SDK DLL (IPC client).
/// Requires Everything.exe to be running in the background.
/// </summary>
public sealed class EverythingApi : IDisposable
{
    private const string EverythingDllName = "Everything64.dll";

    private const uint RequestFlags =
        EverythingRequestFlags.FileName
        | EverythingRequestFlags.Path
        | EverythingRequestFlags.DateModified
        | EverythingRequestFlags.Size;

    private bool _disposed;

    public bool MatchPath { get; set; }
    public bool MatchCase { get; set; }
    public bool MatchWholeWord { get; set; }
    public bool EnableRegex { get; set; }
    public uint Sort { get; set; } = EverythingSort.DateModifiedDescending;

    public IReadOnlyList<EverythingFileInfo> Search(
        string query,
        uint offset = 0,
        uint maxCount = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Everything search is only supported on Windows.");
        }

        Everything_SetMatchPath(MatchPath);
        Everything_SetMatchCase(MatchCase);
        Everything_SetMatchWholeWord(MatchWholeWord);
        Everything_SetRegex(EnableRegex);
        Everything_SetSort(Sort);
        Everything_SetSearchW(query);
        Everything_SetRequestFlags(RequestFlags);
        Everything_SetOffset(offset);
        Everything_SetMax(maxCount);

        if (!Everything_QueryW(true))
        {
            throw Everything_GetLastError() switch
            {
                2 => new EverythingIpcException(),
                _ => new EverythingException($"Everything query failed with error code {Everything_GetLastError()}."),
            };
        }

        var results = new List<EverythingFileInfo>();
        var count = Everything_GetNumResults();
        var buffer = new StringBuilder(512);

        for (uint index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            buffer.Clear();
            Everything_GetResultFullPathNameW(index, buffer, (uint)buffer.Capacity);

            var filePath = buffer.ToString();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                continue;
            }

            Everything_GetResultDateModified(index, out var modifiedTicks);
            Everything_GetResultSize(index, out var size);

            var fileNamePtr = Everything_GetResultFileNameW(index);
            var fileName = fileNamePtr == IntPtr.Zero
                ? Path.GetFileName(filePath)
                : Marshal.PtrToStringUni(fileNamePtr) ?? Path.GetFileName(filePath);

            DateTime modified;
            try
            {
                modified = DateTime.FromFileTime(modifiedTicks);
            }
            catch
            {
                modified = DateTime.MinValue;
            }

            results.Add(new EverythingFileInfo
            {
                FileName = fileName,
                FilePath = filePath,
                Modified = modified,
                Size = size,
                IsFolder = Everything_IsFolderResult(index),
            });
        }

        return results;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Everything_Reset();
        _disposed = true;
    }

    private static class EverythingRequestFlags
    {
        public const uint FileName = 0x00000001;
        public const uint Path = 0x00000002;
        public const uint DateModified = 0x00000040;
        public const uint Size = 0x00000010;
    }

    public static class EverythingSort
    {
        public const uint NameAscending = 1;
        public const uint NameDescending = 2;
        public const uint PathAscending = 3;
        public const uint PathDescending = 4;
        public const uint SizeAscending = 5;
        public const uint SizeDescending = 6;
        public const uint DateModifiedAscending = 13;
        public const uint DateModifiedDescending = 14;
    }

    [DllImport(EverythingDllName, CharSet = CharSet.Unicode)]
    private static extern uint Everything_SetSearchW(string search);

    [DllImport(EverythingDllName)]
    private static extern void Everything_SetMatchPath(bool enable);

    [DllImport(EverythingDllName)]
    private static extern void Everything_SetMatchCase(bool enable);

    [DllImport(EverythingDllName)]
    private static extern void Everything_SetMatchWholeWord(bool enable);

    [DllImport(EverythingDllName)]
    private static extern void Everything_SetRegex(bool enable);

    [DllImport(EverythingDllName)]
    private static extern void Everything_SetMax(uint max);

    [DllImport(EverythingDllName)]
    private static extern void Everything_SetOffset(uint offset);

    [DllImport(EverythingDllName)]
    private static extern uint Everything_GetLastError();

    [DllImport(EverythingDllName)]
    private static extern bool Everything_QueryW(bool wait);

    [DllImport(EverythingDllName)]
    private static extern uint Everything_GetNumResults();

    [DllImport(EverythingDllName)]
    private static extern bool Everything_IsFolderResult(uint index);

    [DllImport(EverythingDllName, CharSet = CharSet.Unicode)]
    private static extern void Everything_GetResultFullPathNameW(uint index, StringBuilder buffer, uint maxCount);

    [DllImport(EverythingDllName, CharSet = CharSet.Unicode)]
    private static extern IntPtr Everything_GetResultFileNameW(uint index);

    [DllImport(EverythingDllName)]
    private static extern bool Everything_GetResultSize(uint index, out long fileSize);

    [DllImport(EverythingDllName)]
    private static extern bool Everything_GetResultDateModified(uint index, out long fileTime);

    [DllImport(EverythingDllName)]
    private static extern void Everything_Reset();

    [DllImport(EverythingDllName)]
    private static extern void Everything_SetSort(uint sortType);

    [DllImport(EverythingDllName)]
    private static extern void Everything_SetRequestFlags(uint requestFlags);
}
