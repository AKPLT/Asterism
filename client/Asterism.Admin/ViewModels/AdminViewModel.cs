using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using Asterism.Admin.Services;
using Asterism.Admin.Services.Exceptions;
using Asterism.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asterism.Admin.ViewModels;

public partial class AdminViewModel : ObservableObject
{
    public const string AllCategoriesLabel = ToolFilter.AllCategoriesLabel;

    private readonly IAdminApiService _adminApiService;

    public ObservableCollection<ToolEntry> Tools { get; } = new();
    public ObservableCollection<string> Categories { get; } = new() { AllCategoriesLabel };
    public ICollectionView ToolsView { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSearchText))]
    private string searchText = "";

    public bool HasSearchText => SearchText.Length > 0;

    [ObservableProperty]
    private string selectedCategory = AllCategoriesLabel;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isLoading;

    public AdminViewModel(IAdminApiService adminApiService)
    {
        _adminApiService = adminApiService;

        ToolsView = CollectionViewSource.GetDefaultView(Tools);
        ToolsView.Filter = FilterPredicate;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var tools = await _adminApiService.GetToolsAsync();
            Tools.Clear();
            foreach (var tool in tools)
            {
                Tools.Add(tool);
            }

            RebuildCategories();
            ToolsView.Refresh();
        }
        catch (AdminApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(ToolEntry tool)
    {
        var confirm = MessageBox.Show(
            $"'{tool.Name}' をツール一覧から削除しますか？",
            "削除の確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        var purgeResult = MessageBox.Show(
            "サーバー上のZIPファイルとアイコン画像も一緒に削除しますか？\n\n「はい」→ ファイルも削除\n「いいえ」→ 一覧からのみ削除（ファイルは残す）",
            "関連ファイルの削除", MessageBoxButton.YesNo, MessageBoxImage.Question);
        var purge = purgeResult == MessageBoxResult.Yes;

        try
        {
            await _adminApiService.DeleteToolAsync(tool.Id, purge);
            await LoadAsync();
        }
        catch (AdminApiException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void RebuildCategories()
    {
        var current = SelectedCategory;

        Categories.Clear();
        foreach (var category in ToolFilter.BuildCategoryList(Tools.Select(t => t.Category)))
        {
            Categories.Add(category);
        }

        SelectedCategory = Categories.Contains(current) ? current : AllCategoriesLabel;
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = "";

    partial void OnSearchTextChanged(string value) => ToolsView.Refresh();

    partial void OnSelectedCategoryChanged(string value) => ToolsView.Refresh();

    private bool FilterPredicate(object obj)
    {
        return obj is ToolEntry tool && ToolFilter.Matches(tool, SearchText, SelectedCategory);
    }
}
