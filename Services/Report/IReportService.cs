using System;
using System.Threading.Tasks;

namespace AbsoluteCinema.Services.Report;

public interface IReportService
{
    public const string ReportsRootPath = "CinemaControlReports";
    
    event Action OnDownloadProgress;
    string GetSessionPath(DateTime from, DateTime to);
    Task<string> GenerateReportFiles(DateTime from, DateTime to, ReportProvider reportProvider);
}