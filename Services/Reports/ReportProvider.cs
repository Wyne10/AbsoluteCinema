using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AbsoluteCinema.Models;
using Microsoft.Extensions.Logging;

namespace AbsoluteCinema.Services.Reports;

public partial class ReportProvider(ILogger logger, string baseUrl = "http://192.168.3.150") : CinemaWebAccessor(baseUrl)
{
    private async Task<byte[]> DownloadReportAsync(string reportPath, ReportFormat format, DateTime? startDate = null, DateTime? endDate = null, Dictionary<string, string>? fields = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting download at {ReportPath}", reportPath);

        await EnsureAuthenticatedAsync(logger, cancellationToken);

        // Initialize report in session
        logger.LogTrace("Initializing report session...");
        var renderResponse = await HttpClient.GetAsync(
            $"/CinemaWeb/Report/Render?path={HttpUtility.UrlEncode(reportPath)}", cancellationToken);

        if (renderResponse.RequestMessage == null || renderResponse.RequestMessage.RequestUri == null)
            throw new Exception("Empty response from report session");

        if (renderResponse.RequestMessage.RequestUri.ToString().Contains("Account/Login"))
        {
            logger.LogDebug("Authentication expired, retrying...");
            IsAuthenticated = false;
            return await DownloadReportAsync(reportPath, format, startDate, endDate, fields);
        }

        // Load report form
        logger.LogTrace("Extracting report form fields...");
        var pageContent = await HttpClient.GetStringAsync("/CinemaWeb/ReportViewerWebForm.aspx", cancellationToken);

        // Extract and modify form fields
        var formFields = ExtractFormFields(pageContent);

        // Find date fields
        var dateFields = formFields
            .Where(kv => kv.Key.Contains("txtValue") &&
                         DateFieldRegex().IsMatch(kv.Value))
            .OrderBy(kv => kv.Key)
            .ToList();

        if (startDate.HasValue && dateFields.Count >= 1)
        {
            formFields[dateFields[0].Key] = startDate.Value.ToString("dd.MM.yyyy 0:00:00");
        }

        if (endDate.HasValue && dateFields.Count >= 2)
        {
            formFields[dateFields[1].Key] = endDate.Value.ToString("dd.MM.yyyy 0:00:00");
        }

        // Apply additional fields
        if (fields != null)
        {
            foreach (var kvp in fields)
                formFields[kvp.Key] = kvp.Value;
        }

        // Submit form
        formFields["ReportViewer1$ctl04$ctl00"] = "View Report";
        formFields["__EVENTTARGET"] = "";
        formFields["__EVENTARGUMENT"] = "";

        var postResponse = await HttpClient.PostAsync(
            "/CinemaWeb/ReportViewerWebForm.aspx",
            new FormUrlEncodedContent(formFields), cancellationToken);

        logger.LogTrace("Extracting session data...");
        var postContent = await postResponse.Content.ReadAsStringAsync(cancellationToken);

        // Extract session and export
        var sessionMatch = ReportSessionRegex().Match(postContent);
        var controlMatch = ControlIdRegex().Match(postContent);

        if (!sessionMatch.Success || !controlMatch.Success)
            throw new Exception("Could not extract report session");

        var fileName = reportPath.Contains('/') ? reportPath.Split('/').Last() : reportPath;
        var exportUrl = $"/CinemaWeb/Reserved.ReportViewerWebControl.axd?" +
                        $"OpType=Export&Format={format}" +
                        $"&ReportSession={sessionMatch.Groups[1].Value}" +
                        $"&ControlID={controlMatch.Groups[1].Value}" +
                        $"&FileName={fileName}" +
                        $"&Culture=1049&CultureOverrides=True&UICulture=1049&UICultureOverrides=True" +
                        $"&ReportStack=1&ContentDisposition=OnlyHtmlInline";

        logger.LogTrace("Exporting report...");
        var exportResponse = await HttpClient.GetAsync(exportUrl, cancellationToken);
        var content = await exportResponse.Content.ReadAsByteArrayAsync(cancellationToken);

        var contentType = exportResponse.Content.Headers.ContentType?.MediaType ?? "";
        if (contentType.Contains("text/html") && content.Length < 10000)
            throw new Exception("Export failed - got HTML response");

        logger.LogInformation("Download successful");
        return content;
    }

    private static Dictionary<string, string> ExtractFormFields(string html)
    {
        var fields = new Dictionary<string, string>();

        // Extract input fields
        var inputMatches = InputFieldRegex().Matches(html);

        foreach (Match match in inputMatches)
        {
            var name = match.Groups[1].Value;
            var value = match.Groups[2].Success ? match.Groups[2].Value : "";

            if (string.IsNullOrEmpty(value))
            {
                var valueFirst = ValueFieldRegex().Match(match.Value);
                if (valueFirst.Success)
                    value = valueFirst.Groups[1].Value;
            }

            if (!fields.ContainsKey(name))
                fields[name] = HttpUtility.HtmlDecode(value);
        }

        // Extract select fields
        var selectMatches = SelectFieldRegex().Matches(html);

        foreach (Match match in selectMatches)
        {
            var name = match.Groups[1].Value;
            var selectedMatch = SelectValueRegex().Match(match.Value);

            if (!fields.ContainsKey(name))
                fields[name] = selectedMatch.Groups[1].Success
                    ? selectedMatch.Groups[1].Value
                    : selectedMatch.Groups[2].Value;
        }

        return fields;
    }

    public async Task DownloadReportToFileAsync(
        string reportPath,
        string outputPath,
        ReportFormat format,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Dictionary<string, string>? fields = null,
        CancellationToken cancellationToken = default)
    {
        var content = await DownloadReportAsync(reportPath, format, startDate, endDate, fields, cancellationToken);
        var directoryPath = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directoryPath))
            Directory.CreateDirectory(directoryPath);
        await File.WriteAllBytesAsync(outputPath, content, cancellationToken);
    }

    [GeneratedRegex(@"\d{2}\.\d{2}\.\d{4}")]
    private static partial Regex DateFieldRegex();

    [GeneratedRegex("ReportSession=([a-z0-9]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ReportSessionRegex();

    [GeneratedRegex("ControlID=([a-f0-9]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ControlIdRegex();

    [GeneratedRegex(@"<input[^>]*name=[""']([^""']+)[""'][^>]*(?:value=[""']([^""']*)[""'])?[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex InputFieldRegex();

    [GeneratedRegex(@"value=[""']([^""']*)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex ValueFieldRegex();

    [GeneratedRegex(@"<select[^>]*name=[""']([^""']+)[""'][^>]*>.*?</select>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex SelectFieldRegex();

    [GeneratedRegex(@"<option[^>]*(?:selected[^>]*value=[""']([^""']*)[""']|value=[""']([^""']*)[""'][^>]*selected)", RegexOptions.IgnoreCase)]
    private static partial Regex SelectValueRegex();
}