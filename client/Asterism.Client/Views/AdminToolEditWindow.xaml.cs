using System.Windows;
using Asterism.Client.ViewModels;
using Asterism.Shared.Models;
using Microsoft.Win32;

namespace Asterism.Client.Views;

public partial class AdminToolEditWindow : Window
{
    private readonly AdminToolEditViewModel _viewModel;

    public AdminToolEditWindow(AdminToolEditViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.SaveCompleted += (_, _) =>
        {
            DialogResult = true;
            Close();
        };
    }

    public void Initialize(ToolEntry? existing) => _viewModel.Initialize(existing);

    private void OnSelectPackageClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = _viewModel.PackageType == PackageType.Zip
                ? "ZIPファイル (*.zip)|*.zip"
                : "インストーラー (*.exe;*.msi)|*.exe;*.msi"
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.PackageFilePath = dialog.FileName;
        }
    }

    private void OnSelectIconClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "画像ファイル (*.png;*.jpg;*.jpeg;*.ico)|*.png;*.jpg;*.jpeg;*.ico"
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.IconFilePath = dialog.FileName;
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
