using AbsoluteCinema.Configuration;
using AbsoluteCinema.Services.Mailing;
using AbsoluteCinema.Services.Movies;
using AbsoluteCinema.Services.Reports;
using AbsoluteCinema.Services.Reports.Monthly;
using AbsoluteCinema.Services.Reports.Quarterly;
using AbsoluteCinema.Services.Reports.Weekly;
using AbsoluteCinema.Services.Schedule;
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
                .Configure<MovieProviderConfiguration>("Poiskkino", builder.Configuration.GetSection("Movie:Poiskkino"))
                .Configure<MovieProviderConfiguration>("Kinoplan", builder.Configuration.GetSection("Movie:Kinoplan"))
                .Configure<DocumentRootConfiguration>("ReportRootPath", builder.Configuration.GetSection("Report:Root"))
                .Configure<DocumentRootConfiguration>("ScheduleRootPath", builder.Configuration.GetSection("Schedule:Root"))
                .Configure<DocumentTemplateConfiguration>("MonthlyReport", builder.Configuration.GetSection("Report:Monthly"))
                .Configure<DocumentTemplateConfiguration>("QuarterlyReport", builder.Configuration.GetSection("Report:Quarterly"))
                .Configure<DocumentTemplateConfiguration>("Repertoire", builder.Configuration.GetSection("Schedule:Repertoire"))
                .Configure<TrailerConfiguration>(builder.Configuration.GetSection("Trailer"))
                .Configure<SmtpConfiguration>(builder.Configuration.GetSection("Mailing:Smtp"));
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
                .AddKeyedTransient<IReportService, WeeklyCardReportService>("weeklyCard")
                .AddKeyedTransient<IReportService, WeeklyCashierReportService>("weeklyCashier")
                .AddKeyedTransient<IReportService, WeeklyRentalsReportService>("weeklyRentals")
                .AddKeyedTransient<IReportService, WeeklyReportService>("weekly")
                .AddKeyedTransient<IReportService, MonthlyPaymentReportService>("monthlyPayment")
                .AddKeyedTransient<IReportService, MonthlyGrossReportService>("monthlyGross")
                .AddKeyedTransient<IReportService, MonthlyReportService>("monthly")
                .AddKeyedTransient<IReportService, QuarterlyReportService>("quarterly")
                // Schedule
                .AddTransient<RepertoireService>()
                .AddTransient<ScheduleSessionService>()
                .AddTransient<IScheduleService, CompleteScheduleService>()
                // Mailing
                .AddSingleton<MailingStorage>()
                .AddSingleton<IEmailService, SmtpEmailService>()
                .AddSingleton<MailingSender>()
                // View models
                .AddTransient<MainWindowViewModel>()
                .AddSingleton<ReportsViewModel>()
                .AddSingleton<ScheduleViewModel>()
                .AddSingleton<TrailersViewModel>()
                .AddSingleton<MailingViewModel>()
                .AddTransient<SettingsViewModel>()
                // Singletons
                .AddSingleton<IMovieProvider<Dtos.Movie>, PoiskinoMovieProvider>()
                .AddSingleton<IMovieProvider<Dtos.KinoplanRelease>, KinoplanMovieProvider>();
        }
    }
}