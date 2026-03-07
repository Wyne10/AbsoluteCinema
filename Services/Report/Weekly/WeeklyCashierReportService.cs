using System;
using System.IO;
using System.Threading.Tasks;
using AbsoluteCinema.Models;

namespace AbsoluteCinema.Services.Report.Weekly;

public class WeeklyCashierReportService : ReportService
{
    private const string ReportPath = "CashReports/CashTodayByUsers";

    public override async Task<string> GenerateReportFiles(DateTime from, DateTime to, ReportProvider reportProvider)
    {
        var sessionPath = GetSessionPath(from, to);

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var newFileName = $"Разбивкой по кассирам {date:dd.MM.yy}.pdf";
            var newFilePath = Path.Combine(sessionPath, newFileName);
            await reportProvider.DownloadReportToFileAsync(ReportPath, newFilePath, ReportFormat.PDF, date);
            
            ProgressDownload();
        }

        return sessionPath;
    }
}