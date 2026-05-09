using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AbsoluteCinema.Dtos;
using Microsoft.Extensions.Logging;

namespace AbsoluteCinema.Services.Movies;

public partial class SessionProvider(ILogger logger, string baseUrl = "http://10.72.58.93") : CinemaWebAccessor(baseUrl)
{
    public async Task<List<CinemaWebSession>> GetSessionsAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching sessions for {Date}", date);

        await EnsureAuthenticatedAsync(logger, cancellationToken);

        var dateStr = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var html = await HttpClient.GetStringAsync($"/CinemaWeb/Session?date={dateStr}", cancellationToken);

        var movieIdMap = ParseMovieIdMap(html);
        var sessions = ParseSessions(html, movieIdMap);

        logger.LogInformation("Fetched {Count} sessions for {Date}", sessions.Count, date);
        return sessions;
    }

    private static Dictionary<string, int> ParseMovieIdMap(string html)
    {
        var map = new Dictionary<string, int>();

        foreach (Match match in MovieBoxRegex().Matches(html))
        {
            if (!int.TryParse(match.Groups[1].Value, out var movieId)) continue;
            var name = match.Groups[2].Value.Trim();
            if (!string.IsNullOrWhiteSpace(name))
                map.TryAdd(name, movieId);
        }

        return map;
    }

    private static List<CinemaWebSession> ParseSessions(string html, Dictionary<string, int> movieIdMap)
    {
        var sessions = new List<CinemaWebSession>();

        // Find each timeline-row and its auditorium ID
        var rowMatches = TimelineRowRegex().Matches(html);
        for (var i = 0; i < rowMatches.Count; i++)
        {
            var rowMatch = rowMatches[i];
            if (!int.TryParse(rowMatch.Groups[1].Value, out var auditoriumId)) continue;

            // Extract content between this row's start and the next row (or end of timeline)
            var rowStart = rowMatch.Index;
            var rowEnd = i + 1 < rowMatches.Count ? rowMatches[i + 1].Index : html.Length;
            var rowHtml = html[rowStart..rowEnd];

            var sessionStarts = SessionRegex().Matches(rowHtml);
            for (var j = 0; j < sessionStarts.Count; j++)
            {
                var start = sessionStarts[j].Index;
                var end = j + 1 < sessionStarts.Count ? sessionStarts[j + 1].Index : rowHtml.Length;
                var sessionHtml = rowHtml[start..end];

                var parsed = ParseSession(sessionHtml, auditoriumId, movieIdMap);
                if (parsed != null)
                    sessions.Add(parsed);
            }
        }

        return sessions;
    }

    private static CinemaWebSession? ParseSession(string sessionHtml, int auditoriumId, Dictionary<string, int> movieIdMap)
    {
        var idMatch = DataIdRegex().Match(sessionHtml);
        if (!idMatch.Success || !int.TryParse(idMatch.Groups[1].Value, out var sessionId)) return null;

        var nameMatch = SessionNameRegex().Match(sessionHtml);
        if (!nameMatch.Success) return null;
        var movieName = nameMatch.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(movieName)) return null;

        var startMatch = StartTimeRegex().Match(sessionHtml);
        var endMatch = EndTimeRegex().Match(sessionHtml);
        if (!startMatch.Success || !endMatch.Success) return null;
        if (!TimeOnly.TryParseExact(startMatch.Groups[1].Value, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime)) return null;
        if (!TimeOnly.TryParseExact(endMatch.Groups[1].Value, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endTime)) return null;

        var durationMatch = DataDurationRegex().Match(sessionHtml);
        int.TryParse(durationMatch.Groups[1].Value, out var duration);

        var priceMatch = PriceRegex().Match(sessionHtml);
        var prices = priceMatch.Success
            ? priceMatch.Groups[1].Value.Split('|').Select(p => int.TryParse(p, out var v) ? v : 0).ToList()
            : [];

        var lockedMatch = DataLockedRegex().Match(sessionHtml);
        var isLocked = lockedMatch.Success && lockedMatch.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase);

        var deletedMatch = DataDeletedRegex().Match(sessionHtml);
        var isDeleted = deletedMatch.Success && deletedMatch.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase);

        movieIdMap.TryGetValue(movieName, out var movieId);

        return new CinemaWebSession(
            Id: sessionId,
            MovieName: movieName,
            MovieId: movieId > 0 ? movieId : null,
            StartTime: startTime,
            EndTime: endTime,
            Duration: duration,
            Prices: prices,
            IsLocked: isLocked,
            IsDeleted: isDeleted,
            AuditoriumId: auditoriumId);
    }

    [GeneratedRegex(@"<div[^>]*data-id=""(\d+)""[^>]*class=""box_movie""[^>]*>\s*<div[^>]*class=""movie-title""[^>]*>\s*([^<]+)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex MovieBoxRegex();

    [GeneratedRegex(@"<div\s+class=""timeline-row""\s+data-id=""(\d+)""", RegexOptions.IgnoreCase)]
    private static partial Regex TimelineRowRegex();

    [GeneratedRegex(@"<div[^>]*data-id=""\d+""[^>]*data-duration=""\d+""[^>]*class=""box_dropped[^""]*""[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex SessionRegex();

    [GeneratedRegex(@"data-id=""(\d+)""", RegexOptions.IgnoreCase)]
    private static partial Regex DataIdRegex();

    [GeneratedRegex(@"class=""session-movie-name""[^>]*>\s*([^<]+)", RegexOptions.IgnoreCase)]
    private static partial Regex SessionNameRegex();

    [GeneratedRegex(@"class=""session-start-time""[^>]*>\s*(\d{1,2}:\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex StartTimeRegex();

    [GeneratedRegex(@"class=""session-end-time""[^>]*>\s*(\d{1,2}:\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex EndTimeRegex();

    [GeneratedRegex(@"data-duration=""(\d+)""", RegexOptions.IgnoreCase)]
    private static partial Regex DataDurationRegex();

    [GeneratedRegex(@"class=""session-price-title""[^>]*>\s*([\d|]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PriceRegex();

    [GeneratedRegex(@"data-locked=""(\w+)""", RegexOptions.IgnoreCase)]
    private static partial Regex DataLockedRegex();

    [GeneratedRegex(@"data-deleted=""(\w+)""", RegexOptions.IgnoreCase)]
    private static partial Regex DataDeletedRegex();
}
