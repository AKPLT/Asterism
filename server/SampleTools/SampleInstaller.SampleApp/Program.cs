// デモ用ダミーインストーラー: サイレント引数を受け取り、自分自身を
// %LOCALAPPDATA%\Asterism\DemoInstalled\SampleInstalledApp\ にコピーして即終了する。
// 実際のインストーラー（Visual Studio等）のインストール後配置をエミュレートするための挙動。

var installDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Asterism", "DemoInstalled", "SampleInstalledApp");

Directory.CreateDirectory(installDir);

var sourcePath = Environment.ProcessPath!;
var destPath = Path.Combine(installDir, "SampleInstalledApp.exe");
File.Copy(sourcePath, destPath, overwrite: true);

Console.WriteLine($"サイレントインストール完了 (引数: {string.Join(' ', args)})");
Console.WriteLine($"インストール先: {destPath}");
return 0;
