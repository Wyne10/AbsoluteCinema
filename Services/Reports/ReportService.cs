using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AbsoluteCinema.Configuration;
using Microsoft.Extensions.Options;

namespace AbsoluteCinema.Services.Reports;

public abstract class ReportService(IOptionsMonitor<DocumentRootConfiguration> rootConfiguration) : IReportService
{
    public event Action? OnDownloadProgress;

    public string GetSessionPath(DateTime startDate, DateTime endDate)
    {
        var reportsRootPath = rootConfiguration.Get("ReportRootPath").RootPath;
        var sessionPath = Path.Combine(reportsRootPath, $"Отчет {startDate:dd.MM.yy} - {endDate:dd.MM.yy}");
        return sessionPath;
    }

    public abstract Task<string> GenerateReportFiles(DateTime from, DateTime to, ReportProvider reportProvider, CancellationToken cancellationToken = default);

    protected void ProgressDownload()
    {
        OnDownloadProgress?.Invoke();
    }
}