using System.Windows;
using Asterism.Client.Services;
using Asterism.Client.ViewModels;
using Asterism.Shared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Asterism.Client.Views;

public partial class AdminWindow : Window
{
    private readonly AdminViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAdminApiService _adminApiService;

    public AdminWindow(AdminViewModel viewModel, IServiceProvider serviceProvider, IAdminApiService adminApiService)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        _adminApiService = adminApiService;
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.LoadCommand.ExecuteAsync(null);
        Closed += (_, _) => _adminApiService.Lock();
    }

    private async void OnAddNewClick(object sender, RoutedEventArgs e)
    {
        var editWindow = _serviceProvider.GetRequiredService<AdminToolEditWindow>();
        editWindow.Owner = this;
        editWindow.Initialize(null);
        if (editWindow.ShowDialog() == true)
        {
            await _viewModel.LoadCommand.ExecuteAsync(null);
        }
    }

    private async void OnEditClick(object sender, RoutedEventArgs e)
    {
        var tool = (ToolEntry)((FrameworkElement)sender).Tag;
        var editWindow = _serviceProvider.GetRequiredService<AdminToolEditWindow>();
        editWindow.Owner = this;
        editWindow.Initialize(tool);
        if (editWindow.ShowDialog() == true)
        {
            await _viewModel.LoadCommand.ExecuteAsync(null);
        }
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        var tool = (ToolEntry)((FrameworkElement)sender).Tag;
        await _viewModel.DeleteCommand.ExecuteAsync(tool);
    }
}
