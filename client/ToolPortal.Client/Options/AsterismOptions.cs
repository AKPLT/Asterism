namespace ToolPortal.Client.Options;

public sealed class ToolPortalOptions
{
    public string ServerBaseUrl { get; set; } = "http://localhost:5000/";
    public int PollingIntervalMinutes { get; set; } = 10;
}
