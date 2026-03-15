using System;
using System.Collections.Generic;
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

namespace AbsoluteCinema.Services.Reports.Quarterly;

public class QuarterlyReportService(
    ILogger<ReportService> logger,
    IOptionsMonitor<DocumentRootConfiguration> rootConfiguration,
    IOptionsMonitor<DocumentTemplateConfiguration> templateConfiguration
    ) : ReportService(rootConfiguration)
{
    private const string ReportPath = "RentalReports/GrossMovieByPeriod";
    
    private DocumentTemplateConfiguration Configuration => templateConfiguration.Get("QuarterlyReport");
    
    public override async Task<string> GenerateReportFiles(DateTime from, DateTime to, ReportProvider reportProvider, CancellationToken cancellationToken = default)
    {
        var sessionPath = GetSessionPath(from, to);

        var newFileName = $"По сборам за период {from:dd.MM.yy} - {to:dd.MM.yy}.xlsx";
        var newFilePath = Path.Combine(sessionPath, newFileName);
        logger.LogInformation("Downloading {FileName}", newFileName);
        await reportProvider.DownloadReportToFileAsync(ReportPath, newFilePath, ReportFormat.EXCELOPENXML, from, to, cancellationToken: cancellationToken);

        ProgressDownload();
        
        var grossMovieData = GrossMovieData.Parse(newFilePath);
        await FillQuarterlyReport(grossMovieData, from, to, cancellationToken);

        ProgressDownload();
        
        return sessionPath;
    } 
    
    private async Task FillQuarterlyReport(IReadOnlyCollection<GrossMovieData> grossMovieData, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var templatePath = Configuration.TemplatePath;
        if (string.IsNullOrWhiteSpace(templatePath))
            throw new Exception("Quarterly report template path is not setup");

        using var workbook = new XLWorkbook(templatePath);
        using var movieProvider = new CinemaWebMovieProvider(logger);
        var worksheet = workbook.Worksheets.First();
        var movies = await movieProvider.GetMoviesAsync(grossMovieData.Select(data => data.MovieName).ToList(), cancellationToken);

        worksheet.Row(12).Cell("C").Value = from.ToString("dd.MM.yy");
        worksheet.Row(12).Cell("E").Value = to.ToString("dd.MM.yy");
        
        var currentRowNumber = 15;
        
        foreach (var movieData in grossMovieData)
        {
            var row = worksheet.Row(currentRowNumber);
            row.InsertRowsBelow(1);
            row.CopyTo(worksheet.Row(currentRowNumber + 1));

            row.Cell("A").Value = currentRowNumber - 14;
            row.Cell("B").Value = $"{movieData.MovieName} {movieData.ScreenType}";
            row.Cell("F").Value = movies.TryGetValue(movieData.MovieName, out var movie) ? movie.CertificateNumber : string.Empty;
            row.Cell("G").Value = movieData.SessionCount;
            row.Cell("H").Value = movieData.ViewerCount;
            row.Cell("I").Value = movieData.Gross;
            
            var rateCell = row.Cell("J");
            rateCell.Value = .0075;
            rateCell.Style.NumberFormat.Format = "0.00%";

            var remunerationCell = row.Cell("K");
            remunerationCell.FormulaA1 = $"I{currentRowNumber}*J{currentRowNumber}";
            worksheet.Row(currentRowNumber + 2).Cell("I").FormulaA1 = $"SUM(I15:I{currentRowNumber})";
            worksheet.Row(currentRowNumber + 2).Cell("K").FormulaA1 = $"SUM(K15:K{currentRowNumber})";
            currentRowNumber++;
        }
        
        var newFileName = $"Отчет об использовании аудиовизуальных произведений {from:dd.MM.yy} - {to:dd.MM.yy}.xlsx";
        var newFilePath = Path.Combine(GetSessionPath(from, to), newFileName);
        workbook.SaveAs(newFilePath);
    }
}