using System.Windows;
using ToolPortal.Client.Converters;
using ToolPortal.Client.Options;
using ToolPortal.Client.Services;
using ToolPortal.Client.ViewModels;
using ToolPortal.Client.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ToolPortal.Client;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg => cfg.AddJsonFile("appsettings.json", optional: false))
            .ConfigureServices((context, services) =>
            {
                services.Configure<ToolPortalOptions>(context.Configuration.GetSection("ToolPortal"));

                services.AddSingleton<IUserSettingsService, UserSettingsService>();

                services.AddHttpClient("ToolPortalServer", (sp, client) =>
                {
                    var userSettings = sp.GetRequiredService<IUserSettingsService>();
                    client.BaseAddress = new Uri(userSettings.ServerBaseUrl);
                    client.Timeout = TimeSpan.FromMinutes(30);
                });

                services.AddSingleton<IManifestService, ManifestService>();
                services.AddSingleton<ILocalStateService, LocalStateService>();
                services.AddSingleton<IInstallService, InstallService>();
                services.AddSingleton<ILaunchService, LaunchService>();
                services.AddSingleton<IUninstallService, UninstallService>();

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();

                services.AddTransient<ServerSettingsDialog>();
            })
            .Build();

        await _host.StartAsync();

        var userSettings = _host.Services.GetRequiredService<IUserSettingsService>();
        StringToImageSourceConverter.BaseUri = new Uri(userSettings.ServerBaseUrl);

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
