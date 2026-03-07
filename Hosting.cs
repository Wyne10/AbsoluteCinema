using AbsoluteCinema.Services.Report;
using AbsoluteCinema.Services.Report.Weekly;
using AbsoluteCinema.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace AbsoluteCinema;

public static class Hosting
{
    extension(HostApplicationBuilder builder)
    {
        public IConfigurationBuilder ConfigureServices(string[] args)
        {
            return builder.Configuration
                .AddCommandLine(args)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.ContentRootPath}.json", optional: true, reloadOnChange: true);
        }
        
        public ILoggingBuilder ConfigureLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();
            
            return builder.Logging
                .ClearProviders()
                .AddSerilog(dispose: true);
        }
        
        public IServiceCollection AddServices()
        {
            return builder.Services
                .AddTransient<WeeklyCardReportService>()
                .AddTransient<WeeklyCashierReportService>()
                .AddTransient<WeeklyRentalsReportService>()
                .AddKeyedTransient<IReportService, WeeklyReportService>("weekly")
                .AddTransient<MainWindowViewModel>()
                .AddTransient<ReportsViewModel>();
        }
    }
}