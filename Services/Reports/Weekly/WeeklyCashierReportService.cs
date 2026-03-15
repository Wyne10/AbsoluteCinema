using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AbsoluteCinema.Configuration;
using AbsoluteCinema.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AbsoluteCinema.Services.Reports.Weekly;

public class WeeklyCashierReportService(ILogger<ReportService> logger, IOptionsMonitor<DocumentRootConfiguration> rootConfiguration) : ReportService(rootConfiguration)
{
    private const string ReportPath = "CashReports/CashTodayByUsers";

    public override async Task<string> GenerateReportFiles(DateTime from, DateTime to, ReportProvider reportProvider, CancellationToken cancellationToken = default)
    {
        var sessionPath = GetSessionPath(from, to);

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var newFileName = $"Разбивкой по кассирам {date:dd.MM.yy}.pdf";
            var newFilePath = Path.Combine(sessionPath, newFileName);
            logger.LogInformation("Downloading {FileName}", newFileName);
            await reportProvider.DownloadReportToFileAsync(ReportPath, newFilePath, ReportFormat.PDF, date, cancellationToken: cancellationToken);
            
            ProgressDownload();
        }

        return sessionPath;
    }
}