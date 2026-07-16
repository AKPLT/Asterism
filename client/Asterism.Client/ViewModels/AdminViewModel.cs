using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using Asterism.Client.Services;
using Asterism.Client.Services.Exceptions;
using Asterism.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asterism.Client.ViewModels;

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
        var result = MessageBox.Show(
            $"'{tool.Name}' を削除しますか？\n（サーバー上の関連ファイルは残ります）",
            "削除の確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _adminApiService.DeleteToolAsync(tool.Id);
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
