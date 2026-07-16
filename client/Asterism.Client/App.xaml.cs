using System.Windows;
using Asterism.Client.Converters;
using Asterism.Client.Options;
using Asterism.Client.Services;
using Asterism.Client.ViewModels;
using Asterism.Client.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Asterism.Client;

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
                services.Configure<AsterismOptions>(context.Configuration.GetSection("Asterism"));

                services.AddHttpClient("AsterismServer", (sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<AsterismOptions>>().Value;
                    client.BaseAddress = new Uri(options.ServerBaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(15);
                });

                services.AddSingleton<IUserSettingsService, UserSettingsService>();
                services.AddSingleton<IManifestService, ManifestService>();
                services.AddSingleton<ILocalStateService, LocalStateService>();
                services.AddSingleton<IInstallService, InstallService>();
                services.AddSingleton<ILaunchService, LaunchService>();
                services.AddSingleton<IUninstallService, UninstallService>();
                services.AddSingleton<IAdminApiService, AdminApiService>();

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();

                services.AddTransient<PasswordDialog>();
                services.AddTransient<AdminViewModel>();
                services.AddTransient<AdminWindow>();
                services.AddTransient<AdminToolEditViewModel>();
                services.AddTransient<AdminToolEditWindow>();
            })
            .Build();

        await _host.StartAsync();

        var options = _host.Services.GetRequiredService<IOptions<AsterismOptions>>().Value;
        StringToImageSourceConverter.BaseUri = new Uri(options.ServerBaseUrl);

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
