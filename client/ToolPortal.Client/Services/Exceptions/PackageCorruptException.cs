namespace ToolPortal.Client.Services.Exceptions;

public sealed class PackageCorruptException : Exception
{
    public PackageCorruptException(string message, Exception? inner = null)
        : base(message, inner) { }
}
