using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AbsoluteCinema.Services.Reports;

public class CompositeReportService : ReportService
{
    private readonly IEnumerable<IReportService> _reportServices;
    
    public CompositeReportService(IEnumerable<IReportService> reportServices)
    {
        _reportServices = reportServices;
        foreach (var reportService in _reportServices) reportService.OnDownloadProgress += ProgressDownload;
    }
    
    public override async Task<string> GenerateReportFiles(DateTime from, DateTime to, ReportProvider reportProvider)
    {
        foreach(var reportService in _reportServices) await reportService.GenerateReportFiles(from, to, reportProvider);
        return GetSessionPath(from, to);
    }
}