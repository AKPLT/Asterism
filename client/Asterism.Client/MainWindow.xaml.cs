using System.Windows;
using Asterism.Client.ViewModels;

namespace Asterism.Client;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
