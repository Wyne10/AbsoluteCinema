using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using AbsoluteCinema.Models;
using Microsoft.Extensions.Logging;

namespace AbsoluteCinema.Services.Report;

public partial class ReportProvider : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private bool _isAuthenticated;

    private readonly ILogger _logger;
    
    public ReportProvider(ILogger logger, string baseUrl = "http://192.168.3.150")
    {
        _logger = logger;
        _baseUrl = baseUrl.TrimEnd('/');
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = true,
            UseCookies = true
        };
        _httpClient = new HttpClient(handler) { BaseAddress = new Uri(_baseUrl) };
    }

    private async Task<bool> LoginAsync(string username = "Администратор", string password = "")
    {
        var loginPage = await _httpClient.GetStringAsync("/CinemaWeb/Account/Login");

        var tokenMatch = ForgeryTokenRegex().Match(loginPage);

        if (!tokenMatch.Success)
            throw new Exception("Anti-forgery token not found");

        var loginData = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("__RequestVerificationToken", tokenMatch.Groups[1].Value),
            new KeyValuePair<string, string>("UserName", username),
            new KeyValuePair<string, string>("Password", password),
            new KeyValuePair<string, string>("RememberMe", "false")
        ]);

        var response = await _httpClient.PostAsync("/CinemaWeb/Account/Login", loginData);
        _isAuthenticated = !response.RequestMessage?.RequestUri?.ToString().Contains("Account/Login") ?? false;

        return _isAuthenticated ? true : throw new Exception("Authentication failed");
    }

    private async Task<byte[]> DownloadReportAsync(string reportPath, ReportFormat format, DateTime? startDate = null, DateTime? endDate = null, Dictionary<string, string>? fields = null)
    {
        _logger.LogDebug("Starting download at {ReportPath}", reportPath);
        if (!_isAuthenticated)
        {
           _logger.LogDebug("Not authenticated, trying to login..."); 
            if (await LoginAsync())
                _logger.LogDebug("Authentication successful");
        }

        // Initialize report in session
        _logger.LogTrace("Initializing report session...");
        var renderResponse = await _httpClient.GetAsync(
            $"/CinemaWeb/Report/Render?path={HttpUtility.UrlEncode(reportPath)}");

        if (renderResponse.RequestMessage == null || renderResponse.RequestMessage.RequestUri == null)
            throw new Exception("Empty response from report session");

        if (renderResponse.RequestMessage.RequestUri.ToString().Contains("Account/Login"))
        {
            _logger.LogDebug("Authentication expired, retrying...");
            _isAuthenticated = false;
            return await DownloadReportAsync(reportPath, format, startDate, endDate, fields);
        }

        // Load report form
        _logger.LogTrace("Extracting report form fields...");
        var pageContent = await _httpClient.GetStringAsync("/CinemaWeb/ReportViewerWebForm.aspx");

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

        var postResponse = await _httpClient.PostAsync(
            "/CinemaWeb/ReportViewerWebForm.aspx",
            new FormUrlEncodedContent(formFields));

        _logger.LogTrace("Extracting session data...");
        var postContent = await postResponse.Content.ReadAsStringAsync();

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

        _logger.LogTrace("Exporting report...");
        var exportResponse = await _httpClient.GetAsync(exportUrl);
        var content = await exportResponse.Content.ReadAsByteArrayAsync();

        var contentType = exportResponse.Content.Headers.ContentType?.MediaType ?? "";
        if (contentType.Contains("text/html") && content.Length < 10000)
            throw new Exception("Export failed - got HTML response");

        _logger.LogDebug("Download successful");
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
        Dictionary<string, string>? fields = null)
    {
        var content = await DownloadReportAsync(reportPath, format, startDate, endDate, fields);
        var directoryPath = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directoryPath))
            Directory.CreateDirectory(directoryPath);
        await File.WriteAllBytesAsync(outputPath, content);
    }

    public void Dispose()
    {
        _httpClient.Dispose(); 
        GC.SuppressFinalize(this);
    }

    [GeneratedRegex(@"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex ForgeryTokenRegex();

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