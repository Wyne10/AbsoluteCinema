using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AbsoluteCinema.Dtos;
using Microsoft.Extensions.Logging;

namespace AbsoluteCinema.Services.Movies;

public partial class CinemaWebMovieProvider(ILogger logger, string baseUrl = "http://192.168.3.150") : CinemaWebAccessor(baseUrl)
{
    public async Task<Dictionary<string, CinemaWebMovie>> GetMoviesAsync(IEnumerable<string> movieNames, CancellationToken cancellationToken = default)
    {
        var movieSet = movieNames.ToHashSet();
        logger.LogInformation("Fetching CinemaWeb movies for {Count} movies", movieSet.Count);

        await EnsureAuthenticatedAsync(logger, cancellationToken);

        logger.LogTrace("Fetching active movie list...");
        var activePage = await HttpClient.GetStringAsync("/CinemaWeb/Movie", cancellationToken);
        var allMovies = ParseMoviesFromTable(activePage);

        logger.LogTrace("Fetching archived movie list...");
        var response = await HttpClient.PostAsync("/CinemaWeb/Movie/IndexSetFilter",
            new FormUrlEncodedContent([new KeyValuePair<string, string>("movieFilter", "InArchive")]), cancellationToken);
        var archivedPage = await response.Content.ReadAsStringAsync(cancellationToken);
        foreach (var kv in ParseMoviesFromTable(archivedPage))
            allMovies.TryAdd(kv.Key, kv.Value);

        var result = allMovies
            .Where(kv => movieSet.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        logger.LogInformation("Fetched {Count} movies", result.Count);
        return result;
    }

    public async Task<List<CinemaWebMovie>> GetActiveMoviesAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching active CinemaWeb movies with details");

        await EnsureAuthenticatedAsync(logger, cancellationToken);

        var html = await HttpClient.GetStringAsync("/CinemaWeb/Movie", cancellationToken);
        var tableMovies = ParseMoviesFromTable(html);

        var movies = new List<CinemaWebMovie>();

        foreach (var movie in tableMovies.Values)
        {
            var details = await GetMovieDetailsAsync(movie.Id, cancellationToken);
            movies.Add(details != null ? movie with { Details = details } : movie);
        }

        logger.LogInformation("Fetched {Count} active movies with details", movies.Count);
        return movies;
    }

    private static Dictionary<string, CinemaWebMovie> ParseMoviesFromTable(string html)
    {
        var movies = new Dictionary<string, CinemaWebMovie>();

        foreach (Match table in TableRegex().Matches(html))
        {
            if (!table.Value.Contains("Прокатчик")) continue;

            var rows = RowRegex().Matches(table.Value).Skip(1);
            foreach (var row in rows)
            {
                var cells = CellRegex().Matches(row.Value);
                if (cells.Count < 9) continue;

                var editMatch = EditLinkRegex().Match(row.Value);
                if (!editMatch.Success) continue;

                var id = int.Parse(editMatch.Groups[1].Value);
                var name = StripTags(cells[0].Groups[1].Value);
                if (string.IsNullOrWhiteSpace(name)) continue;

                var durationStr = StripTags(cells[3].Groups[1].Value);
                int.TryParse(durationStr, out var duration);
                var distributionBegin = ParseDate(StripTags(cells[4].Groups[1].Value));
                var distributionEnd = ParseDate(StripTags(cells[5].Groups[1].Value));
                var ageRestriction = StripTags(cells[6].Groups[1].Value);
                var certificateNumber = StripTags(cells[7].Groups[1].Value);
                var format = StripTags(cells[8].Groups[1].Value);
                var formats = string.IsNullOrWhiteSpace(format) ? [] : format.Split(',', StringSplitOptions.TrimEntries).ToList();
                var isPushkin = PushkinImageRegex().IsMatch(row.Value);

                movies.TryAdd(name, new CinemaWebMovie(id, name, duration, ageRestriction, certificateNumber, formats, distributionBegin, distributionEnd, isPushkin));
            }
        }

        return movies;
    }

    private async Task<CinemaWebMovieDetails?> GetMovieDetailsAsync(int movieId, CancellationToken cancellationToken)
    {
        logger.LogTrace("Fetching movie details for ID {Id}...", movieId);
        var html = await HttpClient.GetStringAsync($"/CinemaWeb/Movie/Edit/{movieId}", cancellationToken);

        var name = ExtractInputValue(html, "Movie_Name");
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return new CinemaWebMovieDetails(
            PushkinId: ExtractInputValue(html, "Movie_PushkinID"),
            Description: ExtractTextareaValue(html, "Movie_Story"),
            Countries: ExtractInputValue(html, "Countries"),
            Genres: ExtractInputValue(html, "Genres"));
    }

    private static string ExtractInputValue(string html, string id)
    {
        var match = Regex.Match(html, $@"id=""{id}""[^>]*value=""([^""]*)""", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (match.Success) return HttpUtility.HtmlDecode(match.Groups[1].Value);

        match = Regex.Match(html, $@"value=""([^""]*)""[^>]*id=""{id}""", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? HttpUtility.HtmlDecode(match.Groups[1].Value) : "";
    }

    private static string ExtractSelectedOption(string html, string id)
    {
        var selectMatch = Regex.Match(html, $@"id=""{id}""[^>]*>.*?</select>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!selectMatch.Success) return "";

        var optionMatch = SelectedOptionRegex().Match(selectMatch.Value);
        return optionMatch.Success ? HttpUtility.HtmlDecode(optionMatch.Groups[1].Value) : "";
    }

    private static string ExtractTextareaValue(string html, string id)
    {
        var match = Regex.Match(html, $@"<textarea[^>]*id=""{id}""[^>]*>(.*?)</textarea>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? HttpUtility.HtmlDecode(match.Groups[1].Value).Trim() : "";
    }

    private static DateOnly? ParseDate(string value) =>
        DateOnly.TryParseExact(value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    private static List<string> ExtractCheckedFormats(string html)
    {
        var formats = new List<string>();
        var checkedIndices = new HashSet<int>();

        foreach (Match match in CheckedFormatRegex().Matches(html))
        {
            if (int.TryParse(match.Groups[1].Value, out var index))
                checkedIndices.Add(index);
        }

        foreach (var index in checkedIndices)
        {
            var nameMatch = Regex.Match(html, $@"name=""MovieTypes\[{index}\]\.Name""[^>]*value=""([^""]*)""", RegexOptions.IgnoreCase);
            if (nameMatch.Success)
                formats.Add(nameMatch.Groups[1].Value);
        }

        return formats;
    }

    private static string StripTags(string html) =>
        TagRegex().Replace(html, "").Replace("&nbsp;", " ").Trim();

    [GeneratedRegex(@"href=""/CinemaWeb/Movie/Edit/(\d+)""", RegexOptions.IgnoreCase)]
    private static partial Regex EditLinkRegex();

    [GeneratedRegex(@"<table[^>]*>.*?</table>", RegexOptions.Singleline)]
    private static partial Regex TableRegex();

    [GeneratedRegex(@"<tr[^>]*>.*?</tr>", RegexOptions.Singleline)]
    private static partial Regex RowRegex();

    [GeneratedRegex(@"<td[^>]*>(.*?)</td>", RegexOptions.Singleline)]
    private static partial Regex CellRegex();

    [GeneratedRegex(@"<option[^>]*selected[^>]*>(.*?)</option>", RegexOptions.IgnoreCase)]
    private static partial Regex SelectedOptionRegex();

    [GeneratedRegex(@"name=""MovieTypesSelection\[(\d+)\]""[^>]*checked", RegexOptions.IgnoreCase)]
    private static partial Regex CheckedFormatRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"<img[^>]*pushkin\.png", RegexOptions.IgnoreCase)]
    private static partial Regex PushkinImageRegex();
}
