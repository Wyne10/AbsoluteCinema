using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AbsoluteCinema.Models;

namespace AbsoluteCinema.Services.Report.Weekly;

public class WeeklyRentalsReportService : ReportService
{
    private const string ReportPath = "CashReports/CashTotalToday";
    private const string ShowRentalsSelector = "ReportViewer1$ctl04$ctl07$ddValue";

    public override async Task<string> GenerateReportFiles(DateTime from, DateTime to, ReportProvider reportProvider)
    {
        var sessionPath = GetSessionPath(from, to);

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var newFileName = $"Сводный кассовый {date:dd.MM.yy}.pdf";
            var newFilePath = Path.Combine(sessionPath, newFileName);
            await reportProvider.DownloadReportToFileAsync(ReportPath, newFilePath, ReportFormat.PDF, date, null,
                new Dictionary<string, string>
                {
                    [ShowRentalsSelector] = "1"
                });
            
            ProgressDownload();
        }

        return sessionPath;
    }
}