using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AbsoluteCinema.Configuration;
using AbsoluteCinema.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AbsoluteCinema.Services.Reports.Monthly;

public class MonthlyPaymentReportService(ILogger<ReportService> logger, IOptionsMonitor<DocumentRootConfiguration> rootConfiguration) : ReportService(rootConfiguration)
{
    private const string ReportPath = "CashReports/PaymentTypesByPeriod";

    public override async Task<string> GenerateReportFiles(DateTime from, DateTime to, ReportProvider reportProvider, CancellationToken cancellationToken = default)
    {
        var sessionPath = GetSessionPath(from, to);

        var newFileName = $"По видам оплат {System.Globalization.DateTimeFormatInfo.CurrentInfo.GetMonthName(from.Month)} {from.Year}.pdf";
        var newFilePath = Path.Combine(sessionPath, newFileName);
        logger.LogInformation("Downloading {FileName}", newFileName);
        await reportProvider.DownloadReportToFileAsync(ReportPath, newFilePath, ReportFormat.PDF, from, to, cancellationToken: cancellationToken);
        
        ProgressDownload();
        
        return sessionPath;
    }
}