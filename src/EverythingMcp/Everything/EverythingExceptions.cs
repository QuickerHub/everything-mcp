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
        : base("Everything IPC failed. Install Everything from https://www.voidtools.com/ and keep the tray client running.")
    {
    }
}
