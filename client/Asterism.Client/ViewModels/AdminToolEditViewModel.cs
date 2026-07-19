using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Asterism.Client.Services;
using Asterism.Client.Services.Exceptions;
using Asterism.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asterism.Client.ViewModels;

public partial class AdminToolEditViewModel : ObservableObject
{
    private readonly IAdminApiService _adminApiService;
    private bool _isNew;
    private bool _idAutoFilled;
    private bool _settingAutoId;
    private string _originalVersion = "";

    [ObservableProperty]
    private string id = "";

    [ObservableProperty]
    private bool isIdReadOnly;

    [ObservableProperty]
    private string name = "";

    [ObservableProperty]
    private string version = "";

    [ObservableProperty]
    private string description = "";

    [ObservableProperty]
    private string category = "";

    [ObservableProperty]
    private string tagsText = "";

    [ObservableProperty]
    private string executablePath = "";

    [ObservableProperty]
    private bool isDisabled;

    [ObservableProperty]
    private string? packageFilePath;

    [ObservableProperty]
    private ImageSource? iconPreview;

    partial void OnIdChanged(string value)
    {
        if (!_settingAutoId)
            _idAutoFilled = false;
    }

    partial void OnPackageFilePathChanged(string? value)
    {
        IconPreview = null;
        if (value is null) return;

        if (_isNew)
        {
            if (string.IsNullOrEmpty(Id) || _idAutoFilled)
            {
                _settingAutoId = true;
                Id = GenerateId(value);
                _settingAutoId = false;
                _idAutoFilled = true;
            }

            TryAutoFillFromPackage(value);
        }

        TryLoadIconPreview(value);
    }

    partial void OnExecutablePathChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(PackageFilePath))
        {
            TryLoadIconPreview(PackageFilePath);
        }
    }

    public IReadOnlyList<string> GetPackageExecutableCandidates()
    {
        if (string.IsNullOrWhiteSpace(PackageFilePath)) return Array.Empty<string>();

        try
        {
            using var archive = ZipFile.OpenRead(PackageFilePath);
            var stripPrefix = ComputeZipStripPrefix(archive);

            return archive.Entries
                .Where(e => Path.GetExtension(e.FullName).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                .Select(e =>
                {
                    var name = e.FullName.Replace('\\', '/');
                    if (stripPrefix is not null && name.StartsWith(stripPrefix, StringComparison.OrdinalIgnoreCase))
                        name = name[stripPrefix.Length..];
                    return name.Replace('/', Path.DirectorySeparatorChar);
                })
                .Distinct()
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception)
        {
            return Array.Empty<string>();
        }
    }

    private static string GenerateId(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        name = Regex.Replace(name, @"[-_. ]\d+(\.\d+)*$", "");
        name = Regex.Replace(name, @"[^a-zA-Z0-9]+", "-").ToLowerInvariant().Trim('-');
        name = Regex.Replace(name, @"-+", "-");
        return name;
    }

    private void TryAutoFillFromPackage(string zipPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var stripPrefix = ComputeZipStripPrefix(archive);

            var exeEntries = archive.Entries
                .Where(e => Path.GetExtension(e.FullName).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (exeEntries.Count != 1) return;

            var exeEntry = exeEntries[0];
            var relativePath = exeEntry.FullName.Replace('\\', '/');
            if (stripPrefix is not null && relativePath.StartsWith(stripPrefix, StringComparison.OrdinalIgnoreCase))
                relativePath = relativePath[stripPrefix.Length..];

            if (string.IsNullOrWhiteSpace(ExecutablePath))
            {
                ExecutablePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
            }
        }
        catch (Exception)
        {
            // zipとして開けない・exeが取り出せない等の場合は自動入力をあきらめ、手動入力に任せる
        }
    }

    private void TryLoadIconPreview(string zipPath)
    {
        if (!OperatingSystem.IsWindows()) return;

        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.exe");
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var stripPrefix = ComputeZipStripPrefix(archive);

            var exeEntry = !string.IsNullOrWhiteSpace(ExecutablePath)
                ? FindEntryByRelativePath(archive, stripPrefix, ExecutablePath)
                : archive.Entries.Count(e => Path.GetExtension(e.FullName).Equals(".exe", StringComparison.OrdinalIgnoreCase)) == 1
                    ? archive.Entries.First(e => Path.GetExtension(e.FullName).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                    : null;

            if (exeEntry is null) return;

            exeEntry.ExtractToFile(tempPath, overwrite: true);

            using var icon = System.Drawing.Icon.ExtractAssociatedIcon(tempPath);
            if (icon is null) return;

            var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            bitmapSource.Freeze();
            IconPreview = bitmapSource;
        }
        catch (Exception)
        {
            // zipとして開けない・アイコン抽出に失敗した等の場合はプレビューをあきらめる
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static string? ComputeZipStripPrefix(ZipArchive archive)
    {
        // インストール時にzip直下のフォルダが1つだけなら剥がされる仕様に合わせて照合する
        var entryNames = archive.Entries.Select(e => e.FullName.Replace('\\', '/')).ToList();
        var topLevelSegments = entryNames.Select(n => n.Split('/')[0]).Distinct().ToList();
        return topLevelSegments.Count == 1 && entryNames.Any(n => n.Contains('/'))
            ? topLevelSegments[0] + "/"
            : null;
    }

    private static ZipArchiveEntry? FindEntryByRelativePath(ZipArchive archive, string? stripPrefix, string relativePath)
    {
        var normalizedTarget = relativePath.Replace('\\', '/').TrimStart('/');
        return archive.Entries.FirstOrDefault(e =>
        {
            var name = e.FullName.Replace('\\', '/');
            if (stripPrefix is not null && name.StartsWith(stripPrefix, StringComparison.OrdinalIgnoreCase))
                name = name[stripPrefix.Length..];
            return name.Equals(normalizedTarget, StringComparison.OrdinalIgnoreCase);
        });
    }

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool isSaving;

    [ObservableProperty]
    private string windowTitle = "新規ツール登録";

    public event EventHandler? SaveCompleted;

    public AdminToolEditViewModel(IAdminApiService adminApiService)
    {
        _adminApiService = adminApiService;
    }

    public void Initialize(ToolEntry? existing, string? initialPackagePath = null)
    {
        _isNew = existing is null;
        IsIdReadOnly = !_isNew;

        if (existing is null)
        {
            Id = "";
            Name = "";
            Version = "";
            Description = "";
            Category = "";
            TagsText = "";
            ExecutablePath = "";
            IsDisabled = false;
            WindowTitle = "新規ツール登録";
            _originalVersion = "";
        }
        else
        {
            Id = existing.Id;
            Name = existing.Name;
            Version = existing.Version;
            Description = existing.Description;
            Category = existing.Category;
            TagsText = string.Join(", ", existing.Tags);
            ExecutablePath = existing.ExecutablePath;
            IsDisabled = existing.IsDisabled;
            WindowTitle = $"ツール編集: {existing.Id}";
            _originalVersion = existing.Version;
        }

        ErrorMessage = null;
        IconPreview = null;
        _idAutoFilled = false;
        _settingAutoId = false;
        PackageFilePath = initialPackagePath;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Id))
        {
            ErrorMessage = "IDは必須です。";
            return;
        }
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "名前は必須です。";
            return;
        }
        if (string.IsNullOrWhiteSpace(Version))
        {
            ErrorMessage = "バージョンは必須です。";
            return;
        }
        if (string.IsNullOrWhiteSpace(ExecutablePath))
        {
            ErrorMessage = "実行ファイルパスは必須です。";
            return;
        }
        if (_isNew && string.IsNullOrWhiteSpace(PackageFilePath))
        {
            ErrorMessage = "パッケージファイルを選択してください。";
            return;
        }
        if (!_isNew && Version != _originalVersion && string.IsNullOrWhiteSpace(PackageFilePath))
        {
            ErrorMessage = "バージョンを変更する場合はパッケージファイルを選択してください。";
            return;
        }

        var entry = new ToolEntry
        {
            Id = Id,
            Name = Name,
            Version = Version,
            Description = Description,
            Category = Category,
            Tags = TagsText
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList(),
            PackageType = PackageType.Zip,
            ExecutablePath = ExecutablePath,
            IsDisabled = IsDisabled
        };

        IsSaving = true;
        try
        {
            if (_isNew)
                await _adminApiService.CreateToolAsync(entry, PackageFilePath!);
            else
                await _adminApiService.UpdateToolAsync(Id, entry, PackageFilePath);

            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (AdminApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool CanSave() => !IsSaving;
}
