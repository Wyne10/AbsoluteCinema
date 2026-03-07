using System;
using System.IO;
using System.Threading.Tasks;
using AbsoluteCinema.Models;

namespace AbsoluteCinema.Services.Report.Weekly;

public class WeeklyCardReportService : ReportService
{
    private const string ReportPath = "RentalReports/MovieByPeriodPushkin";

    public override async Task<string> GenerateReportFiles(DateTime from, DateTime to, ReportProvider reportProvider)
    {
        var sessionPath = GetSessionPath(from, to);

        var newFileName = $"По пушкинской {from:dd.MM.yy} - {to:dd.MM.yy}.pdf";
        var newFilePath = Path.Combine(sessionPath, newFileName);
        await reportProvider.DownloadReportToFileAsync(ReportPath, newFilePath, ReportFormat.PDF, from, to);
        
        ProgressDownload();

        return sessionPath;
    }
}