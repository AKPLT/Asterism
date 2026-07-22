namespace ToolPortal.Client.Services.Exceptions;

public sealed class LaunchFailedException : Exception
{
    public LaunchFailedException(string message, Exception? inner = null)
        : base(message, inner) { }
}
