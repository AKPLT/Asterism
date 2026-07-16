namespace Asterism.Client.Services.Exceptions;

public sealed class InstallerFailedException : Exception
{
    public int? ExitCode { get; }

    public InstallerFailedException(string message, int? exitCode = null, Exception? inner = null)
        : base(message, inner)
    {
        ExitCode = exitCode;
    }
}
