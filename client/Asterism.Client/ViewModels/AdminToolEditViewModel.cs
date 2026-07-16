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
    private PackageType packageType;

    [ObservableProperty]
    private string executablePath = "";

    [ObservableProperty]
    private string? installerArgs;

    [ObservableProperty]
    private string? packageFilePath;

    [ObservableProperty]
    private string? iconFilePath;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool isSaving;

    [ObservableProperty]
    private string windowTitle = "新規ツール登録";

    public Array PackageTypeValues => Enum.GetValues(typeof(PackageType));

    public event EventHandler? SaveCompleted;

    public AdminToolEditViewModel(IAdminApiService adminApiService)
    {
        _adminApiService = adminApiService;
    }

    public void Initialize(ToolEntry? existing)
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
            PackageType = PackageType.Zip;
            ExecutablePath = "";
            InstallerArgs = null;
            WindowTitle = "新規ツール登録";
        }
        else
        {
            Id = existing.Id;
            Name = existing.Name;
            Version = existing.Version;
            Description = existing.Description;
            Category = existing.Category;
            TagsText = string.Join(", ", existing.Tags);
            PackageType = existing.PackageType;
            ExecutablePath = existing.ExecutablePath;
            InstallerArgs = existing.InstallerArgs;
            WindowTitle = $"ツール編集: {existing.Id}";
        }

        PackageFilePath = null;
        IconFilePath = null;
        ErrorMessage = null;
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
        if (_isNew && string.IsNullOrWhiteSpace(PackageFilePath))
        {
            ErrorMessage = "パッケージファイルを選択してください。";
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
            PackageType = PackageType,
            ExecutablePath = ExecutablePath,
            InstallerArgs = string.IsNullOrWhiteSpace(InstallerArgs) ? null : InstallerArgs
        };

        IsSaving = true;
        try
        {
            if (_isNew)
            {
                await _adminApiService.CreateToolAsync(entry, PackageFilePath!, IconFilePath);
            }
            else
            {
                await _adminApiService.UpdateToolAsync(Id, entry, PackageFilePath, IconFilePath);
            }

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
