using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AbsoluteCinema.Models;
using AbsoluteCinema.Services.Movies;
using AbsoluteCinema.Services.Reports;
using AbsoluteCinema.Services.Schedule;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AbsoluteCinema.Services.Mailing;

public sealed class MailingSender(
    IServiceProvider serviceProvider,
    IEmailService emailService,
    ILogger<MailingSender> logger)
{
    public async Task SendRuleAsync(MailingRule rule, IEnumerable<EmailContact> contacts, CancellationToken cancellationToken = default)
    {
        var recipients = contacts.Where(c => rule.ContactIds.Contains(c.Id)).ToList();
        if (recipients.Count == 0)
        {
            logger.LogWarning("No recipients found for rule {RuleId}", rule.Id);
            return;
        }

        var dateRanges = CalculateDateRanges(rule.Frequency);
        var allFiles = new List<string>();

        foreach (var (from, to) in dateRanges)
        {
            var files = await GenerateFilesAsync(rule.ServiceKey, from, to, cancellationToken);
            allFiles.AddRange(files);
        }

        if (allFiles.Count == 0)
        {
            logger.LogWarning("No files generated for rule {RuleId}", rule.Id);
            return;
        }

        var subject = string.IsNullOrWhiteSpace(rule.Subject)
            ? ""
            : rule.Subject;
        var body = "Автоматизированная рассылка - АИС \"ДК Ровесник\"";

        foreach (var contact in recipients)
        {
            logger.LogInformation("Sending {Count} files to {Email}", allFiles.Count, contact.Email);
            await emailService.SendAsync(contact.Email, subject, body, allFiles, cancellationToken);
        }
    }

    private async Task<List<string>> GenerateFilesAsync(string serviceKey, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        string sessionPath;

        if (serviceKey == "schedule")
        {
            var scheduleService = serviceProvider.GetRequiredService<IScheduleService>();
            using var movieProvider = new CinemaWebMovieProvider(logger);
            sessionPath = await scheduleService.GenerateScheduleFiles(from, to, movieProvider, cancellationToken);
        }
        else
        {
            var reportService = serviceProvider.GetRequiredKeyedService<IReportService>(serviceKey);
            using var reportProvider = new ReportProvider(logger);
            sessionPath = await reportService.GenerateReportFiles(from, to, reportProvider, cancellationToken);
        }

        return !Directory.Exists(sessionPath) ? [] : Directory.EnumerateFiles(sessionPath).ToList();
    }

    private static List<(DateTime From, DateTime To)> CalculateDateRanges(MailingFrequency frequency)
    {
        var today = DateTime.Today;

        return frequency switch
        {
            MailingFrequency.Weekly => [GetPreviousWeek(today)],
            MailingFrequency.Monthly => [GetPreviousMonth(today)],
            MailingFrequency.Quarterly => [GetPreviousQuarter(today)],
            MailingFrequency.Semiannual => GetPreviousHalfYear(today),
            _ => []
        };
    }

    private static (DateTime From, DateTime To) GetPreviousWeek(DateTime today)
    {
        var daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        var thisMonday = today.AddDays(-daysFromMonday);
        var prevMonday = thisMonday.AddDays(-7);
        var prevSunday = thisMonday.AddDays(-1);
        return (prevMonday, prevSunday);
    }

    private static (DateTime From, DateTime To) GetPreviousMonth(DateTime today)
    {
        var firstOfThisMonth = new DateTime(today.Year, today.Month, 1);
        var lastOfPrevMonth = firstOfThisMonth.AddDays(-1);
        var firstOfPrevMonth = new DateTime(lastOfPrevMonth.Year, lastOfPrevMonth.Month, 1);
        return (firstOfPrevMonth, lastOfPrevMonth);
    }

    private static (DateTime From, DateTime To) GetPreviousQuarter(DateTime today)
    {
        var currentQuarter = (today.Month - 1) / 3 + 1;
        var prevQuarter = currentQuarter == 1 ? 4 : currentQuarter - 1;
        var year = currentQuarter == 1 ? today.Year - 1 : today.Year;
        var firstMonth = (prevQuarter - 1) * 3 + 1;
        var from = new DateTime(year, firstMonth, 1);
        var to = from.AddMonths(3).AddDays(-1);
        return (from, to);
    }

    private static List<(DateTime From, DateTime To)> GetPreviousHalfYear(DateTime today)
    {
        var currentQuarter = (today.Month - 1) / 3 + 1;

        // Previous two quarters
        var ranges = new List<(DateTime, DateTime)>();
        for (var i = 2; i >= 1; i--)
        {
            var q = currentQuarter - i;
            var year = today.Year;
            if (q <= 0)
            {
                q += 4;
                year--;
            }

            var firstMonth = (q - 1) * 3 + 1;
            var from = new DateTime(year, firstMonth, 1);
            var to = from.AddMonths(3).AddDays(-1);
            ranges.Add((from, to));
        }

        return ranges;
    }
}
