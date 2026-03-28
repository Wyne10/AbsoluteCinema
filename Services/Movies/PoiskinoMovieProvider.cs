using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AbsoluteCinema.Configuration;
using AbsoluteCinema.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AbsoluteCinema.Services.Movies;

public class PoiskinoMovieProvider : IMovieProvider<Movie>
{
    private const string ApiBaseUrl = "https://api.poiskkino.dev/v1.4/movie/search";
    private readonly HttpClient _httpClient = new();

    private readonly IOptionsMonitor<MovieProviderConfiguration> _configurationMonitor;
    private MovieProviderConfiguration Configuration => _configurationMonitor.Get("Poiskkino");
    private readonly ILogger<PoiskinoMovieProvider> _logger;

    private readonly string _movieCacheFilePath;
    private readonly Dictionary<string, JsonElement> _movieCache;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    public PoiskinoMovieProvider(IOptionsMonitor<MovieProviderConfiguration> configurationMonitor, ILogger<PoiskinoMovieProvider> logger)
    {
        _configurationMonitor = configurationMonitor;
        _logger = logger;
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolderPath = Path.Combine(appDataPath, "CinemaControl");
        Directory.CreateDirectory(appFolderPath);
        _movieCacheFilePath = Path.Combine(appFolderPath, "poiskkino_movies.json");

        _movieCache = LoadMovieCache();
        _logger.LogInformation("Loaded {MovieCacheCount} Poiskkino movie cache entries", _movieCache.Count);
    }

    public async Task<Dictionary<string, Movie>> GetMovies(IEnumerable<string> movieNames, CancellationToken cancellationToken = default)
    {
        var movies = new Dictionary<string, Movie>();
        foreach (var movieName in movieNames)
        {
            var movie = await GetMovie(movieName, cancellationToken);
            if (movie != null)
                movies[movieName] = movie;
        }
        return movies;
    }

    private async Task<Movie?> GetMovie(string movieName, CancellationToken cancellationToken = default)
    {
        if (_movieCache.TryGetValue(movieName, out var cached))
            return cached.Deserialize<Movie>();

        var element = await FetchMovieData(movieName, cancellationToken);
        if (element == null)
            return null;

        _movieCache[movieName] = element.Value;
        SaveMovieCache(cancellationToken);
        return element.Value.Deserialize<Movie>();
    }

    private async Task<JsonElement?> FetchMovieData(string movieName, CancellationToken cancellationToken = default)
    {
        var apiToken = Configuration.ApiToken;
        if (string.IsNullOrWhiteSpace(apiToken))
            throw new Exception("API Token is not setup");

        var builder = new UriBuilder(ApiBaseUrl);
        var query = HttpUtility.ParseQueryString(builder.Query);
        query["page"] = "1";
        query["limit"] = "10";
        query["query"] = movieName;
        builder.Query = query.ToString();

        using var request = new HttpRequestMessage(HttpMethod.Get, builder.ToString());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-API-KEY", apiToken);

        _logger.LogInformation("Fetching movie {MovieName} from Poiskkino API...", movieName);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(jsonResponse);

        var docs = document.RootElement.GetProperty("docs");
        JsonElement? bestMatch = null;

        foreach (var doc in docs.EnumerateArray())
        {
            var isSeries = doc.TryGetProperty("isSeries", out var isSeriesProp) && isSeriesProp.GetBoolean();
            if (isSeries) continue;

            if (bestMatch == null)
            {
                bestMatch = doc.Clone();
                continue;
            }

            var currentYear = doc.TryGetProperty("year", out var yearProp) ? yearProp.GetInt32() : 0;
            var bestYear = bestMatch.Value.TryGetProperty("year", out var bestYearProp) ? bestYearProp.GetInt32() : 0;
            if (currentYear > bestYear)
                bestMatch = doc.Clone();
        }

        if (bestMatch == null)
        {
            _logger.LogWarning("No movie found for {MovieName}", movieName);
            return null;
        }

        _logger.LogInformation("Fetched movie {MovieName} successfully", movieName);
        return bestMatch;
    }

    private Dictionary<string, JsonElement> LoadMovieCache()
    {
        if (!File.Exists(_movieCacheFilePath))
            return new Dictionary<string, JsonElement>();

        try
        {
            var json = File.ReadAllText(_movieCacheFilePath);
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new Dictionary<string, JsonElement>();
        }
        catch
        {
            return new Dictionary<string, JsonElement>();
        }
    }

    private async void SaveMovieCache(CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(_movieCache, _jsonSerializerOptions);
            await File.WriteAllTextAsync(_movieCacheFilePath, json, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed saving Poiskkino movie cache");
        }
    }
}