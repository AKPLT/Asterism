namespace ToolPortal.Client.Services.Exceptions;

public sealed class ManifestUnavailableException : Exception
{
    public ManifestUnavailableException(string message, Exception? inner = null)
        : base(message, inner) { }
}
