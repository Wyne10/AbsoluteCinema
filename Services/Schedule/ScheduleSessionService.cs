using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AbsoluteCinema.Configuration;
using AbsoluteCinema.Dtos;
using AbsoluteCinema.Services.Movies;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AbsoluteCinema.Services.Schedule;

public class ScheduleSessionService(
    ILogger<ScheduleService> logger, 
    IOptionsMonitor<DocumentRootConfiguration> rootConfiguration
    ) : ScheduleService(rootConfiguration)
{
    private static readonly XLColor HeaderBackground = XLColor.FromHtml("#6E9ECA");
    private static readonly XLColor TitleColor = XLColor.FromHtml("#4682B4");

    public override async Task<string> GenerateScheduleFiles(DateTime from, DateTime to, CinemaWebMovieProvider movieProvider,
        CancellationToken cancellationToken = default)
    {
        var sessionPath = GetSessionPath(from, to);
        Directory.CreateDirectory(sessionPath);

        using var sessionProvider = new SessionProvider(logger);

        var sessionsByDate = new SortedDictionary<DateOnly, List<CinemaWebSession>>();
        for (var date = DateOnly.FromDateTime(from); date <= DateOnly.FromDateTime(to); date = date.AddDays(1))
        {
            var sessions = await sessionProvider.GetSessionsAsync(date, cancellationToken);
            var activeSessions = sessions
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.StartTime)
                .ToList();
            sessionsByDate[date] = activeSessions;
        }

        var movieNames = sessionsByDate.Values
            .SelectMany(s => s)
            .Select(s => s.MovieName)
            .ToHashSet();
        var movies = await movieProvider.GetMoviesAsync(movieNames, cancellationToken);

        BuildExcel(sessionsByDate, movies, from, to, sessionPath);
        return sessionPath;
    }

    private void BuildExcel(SortedDictionary<DateOnly, List<CinemaWebSession>> sessionsByDate,
        Dictionary<string, CinemaWebMovie> movies,
        DateTime from, DateTime to,
        string sessionPath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("ScheduleWithPrices");

        worksheet.Column("A").Width = 11.25;
        worksheet.Column("B").Width = 35.75;
        worksheet.Column("C").Width = 9.25;
        worksheet.Column("D").Width = 19.12;
        worksheet.Column("E").Width = 15.26;

        var row = 1;

        // Title row
        var fromStr = from.ToString("d MMMM");
        var toStr = to.ToString("d MMMM yyyy");
        worksheet.Cell(row, "A").Value = $"Расписание с {fromStr} по {toStr}";
        worksheet.Range(row, 1, row, 4).Merge();
        worksheet.Row(row).Height = 50;
        var titleStyle = worksheet.Cell(row, "A").Style;
        titleStyle.Font.Bold = true;
        titleStyle.Font.FontSize = 14;
        titleStyle.Font.FontColor = TitleColor;
        titleStyle.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        row++;

        // Header row
        var headers = new[] { "Время начала", "Название фильма", "Прод-сть", "Пушкинская карта", "Цена" };
        for (var col = 1; col <= headers.Length; col++)
        {
            var cell = worksheet.Cell(row, col);
            cell.Value = headers[col - 1];
            cell.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            cell.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 14;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = HeaderBackground;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            cell.Style.Alignment.WrapText = true;
        }
        row++;

        // Data rows per date
        foreach (var (date, sessions) in sessionsByDate)
        {
            // Date separator row
            var dateLabel = date.ToString("d MMMM");
            var dayOfWeek = date.ToString("dddd");
            worksheet.Cell(row, "A").Value = $"{dateLabel} ({dayOfWeek})";
            worksheet.Range(row, 1, row, 5).Merge();
            var dateStyle = worksheet.Cell(row, "A").Style;
            dateStyle.Font.Bold = true;
            dateStyle.Font.FontSize = 15;
            dateStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row++;

            foreach (var session in sessions)
            {
                movies.TryGetValue(session.MovieName, out var movie);
                var ageRestriction = movie?.AgeRestriction;
                var movieLabel = string.IsNullOrWhiteSpace(ageRestriction)
                    ? session.MovieName
                    : $"{session.MovieName} ({ageRestriction})";
                var standardPrice = session.Prices.Count > 0 ? session.Prices[0] : 0;

                var timeCell = worksheet.Cell(row, "A");
                timeCell.Value = session.StartTime.ToTimeSpan();
                timeCell.Style.NumberFormat.Format = "[$-419]h:mm";
                timeCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                timeCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var nameCell = worksheet.Cell(row, "B");
                nameCell.Value = movieLabel;
                nameCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                nameCell.Style.Alignment.WrapText = true;

                worksheet.Cell(row, "C").Value = session.Duration;

                var pushkinCell = worksheet.Cell(row, "D");
                if (movie?.IsPushkin == true)
                {
                    pushkinCell.Value = "+";
                    pushkinCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    pushkinCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                var priceCell = worksheet.Cell(row, "E");
                priceCell.Value = standardPrice;
                priceCell.Style.NumberFormat.Format = @"[$-419]0.00""р.""";
                priceCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                priceCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Apply common font/border to data cells
                for (var col = 1; col <= 5; col++)
                {
                    var cell = worksheet.Cell(row, col);
                    cell.Style.Font.FontSize = 15;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                row++;
            }
        }

        // Footer: contact info
        worksheet.Cell(row, "A").Value = "ТЮЗ ул. Курчатова 25а. телефон: (8-343-77) 7-32-66; 7-22-36";
        worksheet.Range(row, 1, row, 5).Merge();
        var contactStyle = worksheet.Cell(row, "A").Style;
        contactStyle.Font.Bold = true;
        contactStyle.Font.FontSize = 16;
        contactStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        row++;

        // Footer: link text and images
        var footerRow = row;
        worksheet.Cell(row, "A").Value = "ССЫЛКА НА ПОКУПКУ БИЛЕТОВ ОНЛАЙН https://premierzal.ru/theatre/rovesnik/schedule            СМОТРИ ПО ПУШКИНСКОЙ КАРТЕ В КИНО           ";
        worksheet.Range(row, 1, row, 2).Merge();
        var linkStyle = worksheet.Cell(row, "A").Style;
        linkStyle.Font.Bold = true;
        linkStyle.Font.FontSize = 11;
        linkStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        linkStyle.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        linkStyle.Alignment.WrapText = true;

        // Add images if they exist
        var templateDir = Path.Combine(AppContext.BaseDirectory, "Templates");
        var qrPath = Path.Combine(templateDir, "image1.gif");
        var pushkinLogoPath = Path.Combine(templateDir, "image2.png");

        if (File.Exists(qrPath))
            worksheet.AddPicture(qrPath).MoveTo(worksheet.Cell(footerRow, "C")).WithSize(130, 130);

        if (File.Exists(pushkinLogoPath))
            worksheet.AddPicture(pushkinLogoPath).MoveTo(worksheet.Cell(footerRow, "E"), -40, 0).WithSize(160, 140);

        var fileName = $"Расписание {from:dd MMMM} - {to:dd MMMM}.xlsx";
        var filePath = Path.Combine(sessionPath, fileName);
        workbook.SaveAs(filePath);
    }
}
