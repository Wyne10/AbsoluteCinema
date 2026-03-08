using System;
using System.Threading;
using System.Threading.Tasks;

namespace AbsoluteCinema.Services.Reports;

public interface IReportService
{
    public const string ReportsRootPath = "CinemaControlReports";
    
    event Action OnDownloadProgress;
    string GetSessionPath(DateTime from, DateTime to);
    Task<string> GenerateReportFiles(DateTime from, DateTime to, ReportProvider reportProvider, CancellationToken cancellationToken = default);
}