using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

    private void OnWindowDragEnter(object sender, System.Windows.DragEventArgs e)
    {
        var hasZip = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
            && ((string[])e.Data.GetData(System.Windows.DataFormats.FileDrop)!)
                .Any(path => Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase));

        e.Effects = hasZip ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnWindowDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)) return;

        var zipPaths = ((string[])e.Data.GetData(System.Windows.DataFormats.FileDrop)!)
            .Where(path => Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var zipPath in zipPaths)
        {
            var editWindow = _serviceProvider.GetRequiredService<AdminToolEditWindow>();
            editWindow.Owner = this;
            editWindow.Initialize(null, zipPath);
            if (editWindow.ShowDialog() == true)
            {
                await _viewModel.LoadCommand.ExecuteAsync(null);
            }
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

    private void OnAdminHelpClick(object sender, RoutedEventArgs e)
    {
        var win = new AdminHelpWindow { Owner = this };
        win.Show();
    }

    private GridViewColumnHeader? _lastSortHeader;
    private ListSortDirection _lastSortDir = ListSortDirection.Ascending;

    private void OnColumnHeaderClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader header || header.Column is null) return;

        var property = (header.Column.DisplayMemberBinding as Binding)?.Path.Path;
        if (property is null) return;

        if (header == _lastSortHeader)
        {
            _lastSortDir = _lastSortDir == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }
        else
        {
            if (_lastSortHeader != null) _lastSortHeader.Tag = null;
            _lastSortDir = ListSortDirection.Ascending;
        }

        header.Tag = _lastSortDir == ListSortDirection.Ascending ? "asc" : "desc";
        _lastSortHeader = header;

        _viewModel.ToolsView.SortDescriptions.Clear();
        _viewModel.ToolsView.SortDescriptions.Add(new SortDescription(property, _lastSortDir));
    }
}
