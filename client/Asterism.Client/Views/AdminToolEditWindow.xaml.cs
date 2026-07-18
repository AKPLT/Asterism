using System.Windows;
using Asterism.Client.ViewModels;
using Asterism.Shared.Models;

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

    public void Initialize(ToolEntry? existing, string? initialPackagePath = null) =>
        _viewModel.Initialize(existing, initialPackagePath);

    private void OnSelectPackageClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "ZIPファイル (*.zip)|*.zip" };
        if (dialog.ShowDialog() == true)
            _viewModel.PackageFilePath = dialog.FileName;
    }

    private void OnSelectExecutableClick(object sender, RoutedEventArgs e)
    {
        var candidates = _viewModel.GetPackageExecutableCandidates();
        if (candidates.Count == 0)
        {
            MessageBox.Show(
                "パッケージファイルが未選択か、.exeファイルが見つかりませんでした。",
                "実行ファイルの選択", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var picker = new ExecutablePickerWindow(candidates) { Owner = this };
        if (picker.ShowDialog() == true && picker.SelectedPath is not null)
        {
            _viewModel.ExecutablePath = picker.SelectedPath;
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
