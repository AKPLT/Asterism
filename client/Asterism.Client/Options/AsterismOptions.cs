namespace Asterism.Client.Options;

public sealed class AsterismOptions
{
    public string ServerBaseUrl { get; set; } = "http://localhost:5000/";
    public int PollingIntervalMinutes { get; set; } = 10;
}
