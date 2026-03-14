using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AbsoluteCinema.Services.Reports;

public abstract class ReportService : IReportService
{
    public event Action? OnDownloadProgress;

    public string GetSessionPath(DateTime startDate, DateTime endDate)
    {
        var reportsRootPath = Path.Combine(Path.GetTempPath(), IReportService.ReportsRootPath);
        var sessionPath = Path.Combine(reportsRootPath, $"Отчет {startDate:yyyy-MM-dd} - {endDate:yyyy-MM-dd}");
        return sessionPath;
    }

    public abstract Task<string> GenerateReportFiles(DateTime from, DateTime to, ReportProvider reportProvider, CancellationToken cancellationToken = default);

    protected void ProgressDownload()
    {
        OnDownloadProgress?.Invoke();
    }
}