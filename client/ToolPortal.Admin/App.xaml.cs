using System.Windows;
using ToolPortal.Admin.Options;
using ToolPortal.Admin.Services;
using ToolPortal.Admin.ViewModels;
using ToolPortal.Admin.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ToolPortal.Admin;

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
                services.Configure<AdminOptions>(context.Configuration.GetSection("ToolPortal"));

                services.AddHttpClient("ToolPortalServer", (sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<AdminOptions>>().Value;
                    client.BaseAddress = new Uri(options.ServerBaseUrl);
                    client.Timeout = TimeSpan.FromMinutes(30);
                });

                services.AddSingleton<IAdminApiService, AdminApiService>();
                services.AddSingleton<AdminViewModel>();
                services.AddSingleton<AdminWindow>();
                services.AddTransient<AdminToolEditViewModel>();
                services.AddTransient<AdminToolEditWindow>();
            })
            .Build();

        await _host.StartAsync();

        var adminWindow = _host.Services.GetRequiredService<AdminWindow>();
        adminWindow.Show();

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
