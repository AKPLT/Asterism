namespace Asterism.Client.Models;

public sealed class InstalledToolRecord
{
    public string Id { get; set; } = "";
    public string Version { get; set; } = "";
    public PackageType PackageType { get; set; }

    // Zip型: 展開先フォルダの絶対パス / Installer型: 実行ファイルの絶対パス
    public string InstallPath { get; set; } = "";
    public DateTimeOffset InstalledAt { get; set; }
}
