using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AbsoluteCinema.Services.Report;

public abstract class ReportService : IReportService
{
    public event Action? OnDownloadProgress;

    public string GetSessionPath(DateTime startDate, DateTime endDate)
    {
        var reportsRootPath = Path.Combine(Path.GetTempPath(), IReportService.ReportsRootPath);
        var sessionPath = Path.Combine(reportsRootPath, $"Отчет_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}");
        return sessionPath;
    }

    public abstract Task<string> GenerateReportFiles(DateTime from, DateTime to, ReportProvider reportProvider);

    protected void ProgressDownload()
    {
        OnDownloadProgress?.Invoke();
    }
}