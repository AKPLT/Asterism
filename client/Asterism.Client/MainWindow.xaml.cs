using System.IO;
using System.Windows;
using Asterism.Client.ViewModels;
using Asterism.Client.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Asterism.Client;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;
    private System.Windows.Forms.NotifyIcon _trayIcon = null!;
    private bool _exitRequested;

    public MainWindow(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        DataContext = _viewModel;

        Loaded += OnWindowLoaded;
    }

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        InitTrayIcon();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private void InitTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon();

        var exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
        {
            try
            {
                _trayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
            }
            catch
            {
                _trayIcon.Icon = System.Drawing.SystemIcons.Application;
            }
        }
        else
        {
            _trayIcon.Icon = System.Drawing.SystemIcons.Application;
        }

        _trayIcon.Text = "Asterism";
        _trayIcon.Visible = true;
        _trayIcon.DoubleClick += (_, _) => ShowWindow();

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Asterism を開く", null, (_, _) => ShowWindow());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("終了", null, (_, _) => ExitApplication());
        _trayIcon.ContextMenuStrip = menu;

        _viewModel.UpdatesDetected += OnUpdatesDetected;
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _exitRequested = true;
        Application.Current.Shutdown();
    }

    private void OnUpdatesDetected(int count)
    {
        _trayIcon.BalloonTipTitle = "Asterism";
        _trayIcon.BalloonTipText = $"{count}件のツールに更新があります。";
        _trayIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
        _trayIcon.ShowBalloonTip(5000);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_exitRequested)
        {
            base.OnClosing(e);
            return;
        }
        e.Cancel = true;
        Hide();
    }

    protected override void OnClosed(EventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnClosed(e);
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
