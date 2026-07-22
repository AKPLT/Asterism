using System.Windows;
using Asterism.Admin.Options;
using Asterism.Admin.Services;
using Asterism.Admin.ViewModels;
using Asterism.Admin.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Asterism.Admin;

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
                services.Configure<AdminOptions>(context.Configuration.GetSection("Asterism"));

                services.AddHttpClient("AsterismServer", (sp, client) =>
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
