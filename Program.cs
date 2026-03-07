using Avalonia;
using System;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AbsoluteCinema;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task Main(string[] args) {
        var builder = Host.CreateApplicationBuilder(args);

        builder.ConfigureServices(args);
        builder.ConfigureLogger();
        builder.AddServices();

        var host = builder.Build();
        App.Host = host;   

        await host.StartAsync();

        try
        {
            var app = BuildAvaloniaApp();

            app.AfterSetup(_ =>
            {
                if (Application.Current?.ApplicationLifetime 
                    is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                    var avaloniaShutdown = false;
                    desktop.ShutdownRequested += (_, _) => avaloniaShutdown = true;
                    lifetime.ApplicationStopping.Register(() =>
                    {
                        if (avaloniaShutdown) return;
                        Dispatcher.UIThread.InvokeAsync(() => desktop.Shutdown()).Wait();
                    });
                }
            });

            app.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Disaster strike");
        }
        finally
        {
            await host.StopAsync();
            host.Dispose();
        } 
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToDelegate(Log.Debug);
}
