namespace Asterism.Client.Services.Exceptions;

public sealed class AdminApiException : Exception
{
    public AdminApiException(string message, Exception? inner = null)
        : base(message, inner) { }
}
