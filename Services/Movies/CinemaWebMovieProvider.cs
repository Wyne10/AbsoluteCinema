using System.Collections.Generic;
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
        logger.LogInformation("Fetching CinemaWeb movie details for {Count} movies", movieSet.Count);

        if (!IsAuthenticated)
        {
            logger.LogDebug("Not authenticated, trying to login...");
            if (await LoginAsync(cancellationToken: cancellationToken))
                logger.LogDebug("Authentication successful");
        }

        var movieIdMap = await GetMovieIdMapAsync(cancellationToken);
        var result = new Dictionary<string, CinemaWebMovie>();

        foreach (var name in movieSet)
        {
            if (!movieIdMap.TryGetValue(name, out var id))
            {
                logger.LogWarning("Movie not found in CinemaWeb: {Name}", name);
                continue;
            }

            var movie = await GetMovieDetailsAsync(id, cancellationToken);
            if (movie != null)
                result[name] = movie;
        }

        logger.LogInformation("Fetched {Count} movie details", result.Count);
        return result;
    }

    public async Task<List<CinemaWebMovie>> GetActiveMoviesAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching all active CinemaWeb movies");

        if (!IsAuthenticated)
        {
            logger.LogDebug("Not authenticated, trying to login...");
            if (await LoginAsync(cancellationToken: cancellationToken))
                logger.LogDebug("Authentication successful");
        }

        var activeIds = await GetMovieIdsAsync("/CinemaWeb/Movie", cancellationToken);
        var movies = new List<CinemaWebMovie>();

        foreach (var id in activeIds)
        {
            var movie = await GetMovieDetailsAsync(id, cancellationToken);
            if (movie != null)
                movies.Add(movie);
        }

        logger.LogInformation("Fetched {Count} active movies", movies.Count);
        return movies;
    }

    private async Task<Dictionary<string, int>> GetMovieIdMapAsync(CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, int>();

        logger.LogTrace("Fetching active movies list...");
        var activePage = await HttpClient.GetStringAsync("/CinemaWeb/Movie", cancellationToken);
        ParseMovieIds(activePage, map);

        logger.LogTrace("Fetching archived movies list...");
        var response = await HttpClient.PostAsync("/CinemaWeb/Movie/IndexSetFilter",
            new FormUrlEncodedContent([new KeyValuePair<string, string>("movieFilter", "InArchive")]), cancellationToken);
        var archivedPage = await response.Content.ReadAsStringAsync(cancellationToken);
        ParseMovieIds(archivedPage, map);

        logger.LogDebug("Found {Count} movies in CinemaWeb", map.Count);
        return map;
    }

    private async Task<List<int>> GetMovieIdsAsync(string url, CancellationToken cancellationToken)
    {
        var html = await HttpClient.GetStringAsync(url, cancellationToken);
        return EditLinkRegex().Matches(html)
            .Select(m => int.Parse(m.Groups[1].Value))
            .ToList();
    }

    private static void ParseMovieIds(string html, Dictionary<string, int> map)
    {
        foreach (Match match in EditLinkRegex().Matches(html))
        {
            var id = int.Parse(match.Groups[1].Value);
            var nameMatch = MovieNameRegex().Match(match.Value);
            if (!nameMatch.Success) continue;

            var name = StripTags(HttpUtility.HtmlDecode(nameMatch.Groups[1].Value));
            map.TryAdd(name, id);
        }
    }

    private async Task<CinemaWebMovie?> GetMovieDetailsAsync(int movieId, CancellationToken cancellationToken)
    {
        logger.LogTrace("Fetching movie details for ID {Id}...", movieId);
        var html = await HttpClient.GetStringAsync($"/CinemaWeb/Movie/Edit/{movieId}", cancellationToken);

        var name = ExtractInputValue(html, "Movie_Name");
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var durationStr = ExtractInputValue(html, "Movie_DurationInt");
        int.TryParse(durationStr, out var duration);

        var ageRestriction = ExtractSelectedOption(html, "Movie_ParentalControl");
        var certificateNumber = ExtractInputValue(html, "Movie_RentalLicense");
        var pushkinId = ExtractInputValue(html, "Movie_PushkinID");
        var description = ExtractTextareaValue(html, "Movie_Story");
        var country = ExtractInputValue(html, "Countries");
        var genres = ExtractInputValue(html, "Genres");
        var formats = ExtractCheckedFormats(html);

        return new CinemaWebMovie(movieId, name, duration, ageRestriction, certificateNumber, pushkinId, formats, description, country, genres);
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

    [GeneratedRegex(@"<tr[^>]*>.*?<a[^>]*href=""/CinemaWeb/Movie/Edit/(\d+)""[^>]*>.*?</tr>", RegexOptions.Singleline)]
    private static partial Regex EditLinkRegex();

    [GeneratedRegex("<td[^>]*>(.*?)</td>", RegexOptions.Singleline)]
    private static partial Regex MovieNameRegex();

    [GeneratedRegex("<option[^>]*selected[^>]*>(.*?)</option>", RegexOptions.IgnoreCase)]
    private static partial Regex SelectedOptionRegex();

    [GeneratedRegex(@"name=""MovieTypesSelection\[(\d+)\]""[^>]*checked", RegexOptions.IgnoreCase)]
    private static partial Regex CheckedFormatRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();
}