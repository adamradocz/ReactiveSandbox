using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactiveSandbox.Models;
using ReactiveSandbox.Services;
using ReactiveSandbox.ViewModels;
using System.Threading.Tasks;
using System.Windows;

namespace ReactiveSandbox;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
        .ConfigureServices((hostBuilderContext, services) =>
        {
            _ = services.AddSingleton<MainWindowViewModel>()
                .AddSingleton<MainWindow>()
                .AddSingleton<GeneratorService>()
                .AddSingleton<TrackService>()
                .Configure<AppOption>(hostBuilderContext.Configuration.GetSection(nameof(AppOption)));
        })
        .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }
}
