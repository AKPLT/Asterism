using System.IO;
using System.Windows;
using Asterism.Client.Models;
using Asterism.Client.Services;
using Asterism.Client.Services.Exceptions;
using Asterism.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asterism.Client.ViewModels;

public enum ToolCardState
{
    NotInstalled,
    Installed,
    UpdateAvailable
}

public partial class ToolCardViewModel : ObservableObject
{
    private readonly IInstallService _installService;
    private readonly ILaunchService _launchService;
    private readonly IUninstallService _uninstallService;
    private readonly ILocalStateService _localStateService;

    private InstalledToolRecord? _installedRecord;

    public ToolEntry Tool { get; private set; }
    public Action<string>? OnTagClicked { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PrimaryButtonLabel))]
    [NotifyPropertyChangedFor(nameof(IsUpdateAvailable))]
    [NotifyCanExecuteChangedFor(nameof(UninstallCommand))]
    private ToolCardState state;

    public bool IsUpdateAvailable => State == ToolCardState.UpdateAvailable;
    public string VersionText => Tool.Version;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrimaryActionCommand))]
    [NotifyCanExecuteChangedFor(nameof(UninstallCommand))]
    private bool isBusy;

    [ObservableProperty]
    private double progressPercent;

    [ObservableProperty]
    private string? errorMessage;

    public string Name => Tool.Name;
    public string Description => Tool.Description;
    public string Category => Tool.Category;
    public IReadOnlyList<string> Tags => Tool.Tags;
    public string IconUrl => Tool.IconUrl;

    public string PrimaryButtonLabel => State switch
    {
        ToolCardState.NotInstalled => "インストール",
        ToolCardState.UpdateAvailable => "更新",
        _ => "起動"
    };

    public ToolCardViewModel(
        ToolEntry tool,
        IInstallService installService,
        ILaunchService launchService,
        IUninstallService uninstallService,
        ILocalStateService localStateService)
    {
        Tool = tool;
        _installService = installService;
        _launchService = launchService;
        _uninstallService = uninstallService;
        _localStateService = localStateService;

        RefreshState();
    }

    public void UpdateTool(ToolEntry tool)
    {
        Tool = tool;
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(Category));
        OnPropertyChanged(nameof(Tags));
        OnPropertyChanged(nameof(IconUrl));
        OnPropertyChanged(nameof(VersionText));
        RefreshState();
    }

    private void RefreshState()
    {
        _installedRecord = _localStateService.GetRecord(Tool.Id);
        if (_installedRecord == null)
        {
            State = ToolCardState.NotInstalled;
            return;
        }

        var installedVersion = Version.TryParse(_installedRecord.Version, out var iv) ? iv : new Version(0, 0, 0);
        var manifestVersion = Version.TryParse(Tool.Version, out var mv) ? mv : new Version(0, 0, 0);

        State = manifestVersion > installedVersion ? ToolCardState.UpdateAvailable : ToolCardState.Installed;
    }

    [RelayCommand(CanExecute = nameof(CanPrimaryAction))]
    private async Task PrimaryActionAsync(CancellationToken ct)
    {
        if (State == ToolCardState.Installed && _installedRecord != null)
        {
            LaunchInternal();
            return;
        }

        await InstallOrUpdateInternalAsync(ct);
    }

    private bool CanPrimaryAction() => !IsBusy;

    private void LaunchInternal()
    {
        ErrorMessage = null;
        try
        {
            _launchService.Launch(Tool, _installedRecord!);
        }
        catch (LaunchFailedException ex)
        {
            ErrorMessage = ex.Message;
            RefreshState();
        }
    }

    private async Task InstallOrUpdateInternalAsync(CancellationToken ct)
    {
        IsBusy = true;
        ErrorMessage = null;
        ProgressPercent = 0;

        try
        {
            var progress = new Progress<double>(p => ProgressPercent = p);
            _installedRecord = await _installService.InstallOrUpdateAsync(Tool, progress, ct);
            State = ToolCardState.Installed;
        }
        catch (PackageCorruptException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (InstallerFailedException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanUninstall))]
    private void Uninstall()
    {
        if (_installedRecord == null)
        {
            return;
        }

        try
        {
            var outcome = _uninstallService.Uninstall(Tool, _installedRecord);
            _installedRecord = null;
            State = ToolCardState.NotInstalled;

            if (outcome == UninstallOutcome.ManualActionRequired)
            {
                MessageBox.Show(
                    "このツールは既製インストーラーで導入されています。Windowsの「アプリと機能」から削除してください。",
                    "手動でのアンインストールが必要です",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = "ツールが実行中の可能性があります。終了してから再度お試しください。";
        }
    }

    private bool CanUninstall() => !IsBusy && State != ToolCardState.NotInstalled;

    [RelayCommand]
    private void SelectTag(string tag) => OnTagClicked?.Invoke(tag);
}
