using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AbsoluteCinema.Configuration;
using AbsoluteCinema.Dtos;
using AbsoluteCinema.Models;
using AbsoluteCinema.Services.Movies;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace AbsoluteCinema.Services.Reports.Monthly;

public class MonthlyGrossReportService(ILogger<ReportService> logger, IOptionsMonitor<ReportConfiguration> configurationMonitor, IMovieProvider movieProvider) : ReportService
{
    private const string ReportPath = "RentalReports/GrossMovieByPeriod";
    private const string CardReportPath = "RentalReports/MovieByPeriodPushkin";

    private ReportConfiguration Configuration => configurationMonitor.Get("MonthlyReport");

    public override async Task<string> GenerateReportFiles(DateTime from, DateTime to, ReportProvider reportProvider, CancellationToken cancellationToken = default)
    {
        var sessionPath = GetSessionPath(from, to);

        var newFileName = $"По сборам за период {from:dd.MM.yy} - {to:dd.MM.yy}.xlsx";
        var newFilePath = Path.Combine(sessionPath, newFileName);
        logger.LogInformation("Downloading {FileName}", newFileName);
        await reportProvider.DownloadReportToFileAsync(ReportPath, newFilePath, ReportFormat.EXCELOPENXML, from, to, cancellationToken: cancellationToken);

        ProgressDownload();
        
        var grossMovieData = GrossMovieData.Parse(newFilePath);
        await FillMonthlyReport(grossMovieData, from, to, reportProvider, cancellationToken);
        
        ProgressDownload();

        return sessionPath;
    }

    private async Task FillMonthlyReport(IReadOnlyCollection<GrossMovieData> grossMovieData, DateTime from, DateTime to, ReportProvider reportProvider, CancellationToken cancellationToken = default)
    {
        var templatePath = Configuration.TemplatePath;
        if (string.IsNullOrWhiteSpace(templatePath))
            throw new Exception("Monthly report template path is not setup");

        using var document = DocX.Load(templatePath);

        var movies = await movieProvider.GetMovies(grossMovieData.Select(data => data.MovieName), cancellationToken);
        var russianMovies = grossMovieData.Where(data => movies[data.MovieName].IsRussian()).ToImmutableHashSet();
        var foreignMovies = grossMovieData.Where(data => !movies[data.MovieName].IsRussian()).ToImmutableHashSet();

        var sessionTotal = grossMovieData.Sum(data => data.SessionCount);
        var viewerTotal = grossMovieData.Sum(data => data.ViewerCount);
        var sessionChildren = grossMovieData
            .Where(data => movies[data.MovieName].AreChildrenAvailable())
            .Sum(data => data.SessionCount);
        var viewerChildren = grossMovieData
            .Where(data => movies[data.MovieName].AreChildrenAvailable())
            .Sum(data => data.ViewerCount);
        var sessionTeenagers = (int)((sessionTotal - sessionChildren) * 0.7);
        var viewerTeenagers = (int)((viewerTotal - viewerChildren) * 0.7);
        var sessionAdults = sessionTotal - sessionChildren - sessionTeenagers;
        var viewerAdults = viewerTotal - viewerChildren - viewerTeenagers;

        document.ReplaceText(new StringReplaceTextOptions
            { SearchValue = "{{month}}", NewValue = System.Globalization.DateTimeFormatInfo.CurrentInfo.GetMonthName(from.Month) });
        document.ReplaceText(new StringReplaceTextOptions
            { SearchValue = "{{year}}", NewValue = from.Year.ToString() }); 
        document.ReplaceText(new StringReplaceTextOptions 
            { SearchValue = "{{session_total}}", NewValue = sessionTotal.ToString() });
        document.ReplaceText(new StringReplaceTextOptions 
            { SearchValue = "{{viewer_total}}", NewValue = viewerTotal.ToString() });
        document.ReplaceText(new StringReplaceTextOptions 
            { SearchValue = "{{movie_russian}}", NewValue = russianMovies.Count.ToString() });
        document.ReplaceText(new StringReplaceTextOptions 
            { SearchValue = "{{viewer_card}}", NewValue = (await GetCardViewerCount(from, to, reportProvider, cancellationToken)).ToString() });
        document.ReplaceText(new StringReplaceTextOptions 
            { SearchValue = "{{session_russian}}", NewValue = russianMovies.Sum(data => data.SessionCount).ToString() });
        document.ReplaceText(new StringReplaceTextOptions 
            { SearchValue = "{{viewer_russian}}", NewValue = russianMovies.Sum(data => data.ViewerCount).ToString() });
        document.ReplaceText(new StringReplaceTextOptions 
            { SearchValue = "{{movie_foreign}}", NewValue = foreignMovies.Count.ToString() });
        document.ReplaceText(new StringReplaceTextOptions 
            { SearchValue = "{{session_foreign}}", NewValue = foreignMovies.Sum(data => data.SessionCount).ToString() });
        document.ReplaceText(new StringReplaceTextOptions 
            { SearchValue = "{{viewer_foreign}}", NewValue = foreignMovies.Sum(data => data.ViewerCount).ToString() });
        document.ReplaceText(new StringReplaceTextOptions 
            { SearchValue = "{{session_children}}", NewValue = sessionChildren.ToString() });
        document.ReplaceText(new StringReplaceTextOptions 
            { SearchValue = "{{viewer_children}}", NewValue = viewerChildren.ToString() }); 
        document.ReplaceText(new StringReplaceTextOptions 
            { SearchValue = "{{session_teenagers}}", NewValue = sessionTeenagers.ToString() });
        document.ReplaceText(new StringReplaceTextOptions 
            { SearchValue = "{{viewer_teenagers}}", NewValue = viewerTeenagers.ToString() });
        document.ReplaceText(new StringReplaceTextOptions 
            { SearchValue = "{{session_adults}}", NewValue = sessionAdults.ToString() });
        document.ReplaceText(new StringReplaceTextOptions 
            { SearchValue = "{{viewer_adults}}", NewValue = viewerAdults.ToString() }); 
        
        var newFileName = $"Таблица отчетности в УК {System.Globalization.DateTimeFormatInfo.CurrentInfo.GetMonthName(from.Month)} {from:yyyy}г.docx";
        var newFilePath = Path.Combine(GetSessionPath(from, to), newFileName);
        document.SaveAs(newFilePath);
    }

    private async Task<int> GetCardViewerCount(DateTime from, DateTime to, ReportProvider reportProvider, CancellationToken cancellationToken = default)
    {
        var sessionPath = GetSessionPath(from, to);
        
        var newFileName = $"По пушкинской {from:dd.MM.yy} - {to:dd.MM.yy}.xlsx";
        var newFilePath = Path.Combine(sessionPath, newFileName);
        logger.LogInformation("Downloading {FileName}", newFileName);
        await reportProvider.DownloadReportToFileAsync(CardReportPath, newFilePath, ReportFormat.EXCELOPENXML, from, to, cancellationToken: cancellationToken);
        
        ProgressDownload();
        
        using var workbook = new XLWorkbook(newFilePath);
        var worksheet = workbook.Worksheets.First();
        
        var lastRow = worksheet.LastRowUsed();
        if (lastRow == null || lastRow.Cell(4).IsEmpty())
            return 0;
        return lastRow.Cell(4).GetValue<int>();
    }
}