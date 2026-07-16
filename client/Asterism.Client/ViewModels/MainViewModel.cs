using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Data;
using System.Windows.Threading;
using Asterism.Client.Options;
using Asterism.Client.Services;
using Asterism.Client.Services.Exceptions;
using Asterism.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Microsoft.Win32;

namespace Asterism.Client.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public const string AllCategoriesLabel = ToolFilter.AllCategoriesLabel;

    private readonly IManifestService _manifestService;
    private readonly ILocalStateService _localStateService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IInstallService _installService;
    private readonly ILaunchService _launchService;
    private readonly IUninstallService _uninstallService;
    private readonly DispatcherTimer _pollingTimer;
    private bool _isShowingConnectionBanner;

    public ObservableCollection<ToolCardViewModel> AllTools { get; } = new();
    public ObservableCollection<string> Categories { get; } = new() { AllCategoriesLabel };
    public ICollectionView ToolsView { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSearchText))]
    private string searchText = "";

    public bool HasSearchText => SearchText.Length > 0;

    [ObservableProperty]
    private string selectedCategory = AllCategoriesLabel;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? bannerMessage;

    [ObservableProperty]
    private string installDirectory = "";

    [ObservableProperty]
    private bool isBannerError;

    public MainViewModel(
        IManifestService manifestService,
        ILocalStateService localStateService,
        IUserSettingsService userSettingsService,
        IInstallService installService,
        ILaunchService launchService,
        IUninstallService uninstallService,
        IOptions<AsterismOptions> options)
    {
        _manifestService = manifestService;
        _localStateService = localStateService;
        _userSettingsService = userSettingsService;
        _installService = installService;
        _launchService = launchService;
        _uninstallService = uninstallService;
        InstallDirectory = _localStateService.ToolsRootDirectory;

        ToolsView = CollectionViewSource.GetDefaultView(AllTools);
        ToolsView.Filter = FilterPredicate;

        _pollingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.PollingIntervalMinutes))
        };
        _pollingTimer.Tick += async (_, _) => await RefreshManifestCoreAsync(isBackground: true);
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        await RefreshManifestCoreAsync(isBackground: false);
        _pollingTimer.Start();
    }

    [RelayCommand]
    public async Task RefreshManifestAsync()
    {
        await RefreshManifestCoreAsync(isBackground: false);
    }

    private async Task RefreshManifestCoreAsync(bool isBackground)
    {
        if (!isBackground)
        {
            IsLoading = true;
        }

        try
        {
            var manifest = await _manifestService.GetManifestAsync();
            ApplyManifest(manifest.Tools.Select(t => t).ToList(), isOffline: false);
        }
        catch (ManifestUnavailableException)
        {
            if (isBackground)
            {
                BannerMessage = "サーバーに接続できません（バックグラウンド更新確認）。";
                IsBannerError = true;
                _isShowingConnectionBanner = true;
                return;
            }

            var cached = _manifestService.LoadCachedManifest();
            if (cached != null)
            {
                ApplyManifest(cached.Tools, isOffline: true);
            }
            else
            {
                BannerMessage = "サーバーに接続できません。再試行してください。";
                IsBannerError = true;
                _isShowingConnectionBanner = true;
            }
        }
        finally
        {
            if (!isBackground)
            {
                IsLoading = false;
            }
        }
    }

    private void ApplyManifest(IReadOnlyList<ToolEntry> tools, bool isOffline)
    {
        var incomingIds = tools.Select(t => t.Id).ToHashSet();

        foreach (var stale in AllTools.Where(c => !incomingIds.Contains(c.Tool.Id)).ToList())
        {
            stale.PropertyChanged -= OnToolCardPropertyChanged;
            AllTools.Remove(stale);
        }

        foreach (var tool in tools)
        {
            var card = AllTools.FirstOrDefault(c => c.Tool.Id == tool.Id);
            if (card == null)
            {
                card = new ToolCardViewModel(tool, _installService, _launchService, _uninstallService, _localStateService);
                card.OnTagClicked = tag => SearchText = tag;
                card.PropertyChanged += OnToolCardPropertyChanged;
                AllTools.Add(card);
            }
            else if (!card.IsBusy)
            {
                card.UpdateTool(tool);
            }
        }

        RebuildCategories();

        if (isOffline)
        {
            BannerMessage = "オフラインキャッシュを表示中です。再試行してください。";
            IsBannerError = false;
            _isShowingConnectionBanner = true;
        }
        else
        {
            _isShowingConnectionBanner = false;
            UpdateBannerForToolStates();
        }

        ToolsView.Refresh();
    }

    private void OnToolCardPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ToolCardViewModel.State) || _isShowingConnectionBanner)
        {
            return;
        }

        UpdateBannerForToolStates();
    }

    private void UpdateBannerForToolStates()
    {
        var updateCount = AllTools.Count(c => c.State == ToolCardState.UpdateAvailable);
        BannerMessage = updateCount > 0 ? $"{updateCount}件のツールに更新があります。" : null;
        IsBannerError = false;
    }

    private void RebuildCategories()
    {
        var current = SelectedCategory;

        Categories.Clear();
        foreach (var category in ToolFilter.BuildCategoryList(AllTools.Select(c => c.Category)))
        {
            Categories.Add(category);
        }

        SelectedCategory = Categories.Contains(current) ? current : AllCategoriesLabel;
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = "";

    [RelayCommand]
    private void OpenInstallFolder()
    {
        var dir = _localStateService.ToolsRootDirectory;
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
    }

    [RelayCommand]
    private void ChangeInstallFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "インストール先フォルダの選択",
            InitialDirectory = _localStateService.ToolsRootDirectory
        };

        if (dialog.ShowDialog() != true) return;

        _userSettingsService.ToolsDirectory = dialog.FolderName;
        _userSettingsService.Save();
        InstallDirectory = _localStateService.ToolsRootDirectory;
    }

    partial void OnSearchTextChanged(string value)
    {
        ToolsView.Refresh();
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        ToolsView.Refresh();
    }

    private bool FilterPredicate(object obj)
    {
        return obj is ToolCardViewModel card && ToolFilter.Matches(card.Tool, SearchText, SelectedCategory);
    }
}
