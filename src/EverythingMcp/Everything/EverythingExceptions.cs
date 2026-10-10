namespace EverythingMcp.Everything;

public class EverythingException : Exception
{
    public EverythingException(string message) : base(message)
    {
    }

    public EverythingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class EverythingIpcException : EverythingException
{
    public EverythingIpcException()
        : base(
            "Everything IPC failed. Install Everything 1.4.x from https://www.voidtools.com/ "
            + "and keep the tray client running. Call the install_guide or status MCP tool for winget steps.")
    {
    }
}
