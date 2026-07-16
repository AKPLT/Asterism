using System.Windows;
using Asterism.Client.ViewModels;
using Asterism.Client.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Asterism.Client;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private void OnAdminModeClick(object sender, RoutedEventArgs e)
    {
        var dialog = _serviceProvider.GetRequiredService<PasswordDialog>();
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            var adminWindow = _serviceProvider.GetRequiredService<AdminWindow>();
            adminWindow.Owner = this;
            adminWindow.Show();
        }
    }
}
