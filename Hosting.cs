using AbsoluteCinema.Configuration;
using AbsoluteCinema.Services.Movies;
using AbsoluteCinema.Services.Reports;
using AbsoluteCinema.Services.Reports.Monthly;
using AbsoluteCinema.Services.Reports.Quarterly;
using AbsoluteCinema.Services.Reports.Weekly;
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
        public IConfigurationBuilder SetupConfiguration(string[] args)
        {
            return builder.Configuration
                .AddCommandLine(args)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.ContentRootPath}.json", optional: true, reloadOnChange: true);
        }

        public IServiceCollection ConfigureServices()
        {
            return builder.Services
                .Configure<MovieProviderConfiguration>(builder.Configuration.GetSection("Movie"))
                .Configure<ReportConfiguration>("MonthlyReport", builder.Configuration.GetSection("Report:Monthly"))
                .Configure<ReportConfiguration>("QuarterlyReport", builder.Configuration.GetSection("Report:Quarterly"));
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
                // Reports
                .AddTransient<WeeklyCardReportService>()
                .AddTransient<WeeklyCashierReportService>()
                .AddTransient<WeeklyRentalsReportService>()
                .AddKeyedTransient<IReportService, WeeklyReportService>("weekly")
                .AddTransient<MonthlyPaymentReportService>()
                .AddTransient<MonthlyGrossReportService>()
                .AddKeyedTransient<IReportService, MonthlyReportService>("monthly")
                .AddKeyedTransient<IReportService, QuarterlyReportService>("quarterly")
                // View models
                .AddTransient<MainWindowViewModel>()
                .AddTransient<ReportsViewModel>()
                // Singletons
                .AddSingleton<IMovieProvider, MovieProvider>();
        }
    }
}