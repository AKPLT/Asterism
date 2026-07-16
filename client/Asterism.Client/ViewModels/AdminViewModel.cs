using System.Collections.ObjectModel;
using System.Windows;
using Asterism.Client.Services;
using Asterism.Client.Services.Exceptions;
using Asterism.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asterism.Client.ViewModels;

public partial class AdminViewModel : ObservableObject
{
    private readonly IAdminApiService _adminApiService;

    public ObservableCollection<ToolEntry> Tools { get; } = new();

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isLoading;

    public AdminViewModel(IAdminApiService adminApiService)
    {
        _adminApiService = adminApiService;
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
}
