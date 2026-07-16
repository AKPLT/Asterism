using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;
using Asterism.Client.Options;
using Asterism.Client.Services;
using Asterism.Client.Services.Exceptions;
using Asterism.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;

namespace Asterism.Client.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public const string AllCategoriesLabel = "すべて";

    private readonly IManifestService _manifestService;
    private readonly ILocalStateService _localStateService;
    private readonly IInstallService _installService;
    private readonly ILaunchService _launchService;
    private readonly IUninstallService _uninstallService;
    private readonly DispatcherTimer _pollingTimer;
    private bool _isShowingConnectionBanner;

    public ObservableCollection<ToolCardViewModel> AllTools { get; } = new();
    public ObservableCollection<string> Categories { get; } = new() { AllCategoriesLabel };
    public ICollectionView ToolsView { get; }

    [ObservableProperty]
    private string searchText = "";

    [ObservableProperty]
    private string selectedCategory = AllCategoriesLabel;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? bannerMessage;

    [ObservableProperty]
    private bool isBannerError;

    public MainViewModel(
        IManifestService manifestService,
        ILocalStateService localStateService,
        IInstallService installService,
        ILaunchService launchService,
        IUninstallService uninstallService,
        IOptions<AsterismOptions> options)
    {
        _manifestService = manifestService;
        _localStateService = localStateService;
        _installService = installService;
        _launchService = launchService;
        _uninstallService = uninstallService;

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
        Categories.Add(AllCategoriesLabel);
        foreach (var category in AllTools.Select(c => c.Category).Distinct().OrderBy(c => c, StringComparer.CurrentCulture))
        {
            Categories.Add(category);
        }

        SelectedCategory = Categories.Contains(current) ? current : AllCategoriesLabel;
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
        if (obj is not ToolCardViewModel card)
        {
            return false;
        }

        var categoryOk = SelectedCategory == AllCategoriesLabel || card.Category == SelectedCategory;

        var text = SearchText?.Trim() ?? "";
        var textOk = text.Length == 0
            || card.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
            || card.Description.Contains(text, StringComparison.OrdinalIgnoreCase)
            || card.Tags.Any(t => t.Contains(text, StringComparison.OrdinalIgnoreCase));

        return categoryOk && textOk;
    }
}
