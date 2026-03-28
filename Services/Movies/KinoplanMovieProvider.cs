using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AbsoluteCinema.Configuration;
using AbsoluteCinema.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AbsoluteCinema.Services.Movies;

public class KinoplanMovieProvider : IMovieProvider<KinoplanRelease>
{
    private const string BaseUrl = "https://web.kinoplan24.ru";
    private readonly HttpClient _httpClient = new();

    private readonly IOptionsMonitor<MovieProviderConfiguration> _configurationMonitor;
    private MovieProviderConfiguration Configuration => _configurationMonitor.Get("Kinoplan");
    private readonly ILogger<KinoplanMovieProvider> _logger;

    private readonly string _movieCacheFilePath;
    private readonly Dictionary<string, JsonElement> _movieCache;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    public KinoplanMovieProvider(IOptionsMonitor<MovieProviderConfiguration> configurationMonitor, ILogger<KinoplanMovieProvider> logger)
    {
        _configurationMonitor = configurationMonitor;
        _logger = logger;
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolderPath = Path.Combine(appDataPath, "CinemaControl");
        Directory.CreateDirectory(appFolderPath);
        _movieCacheFilePath = Path.Combine(appFolderPath, "kinoplan_movies.json");

        _movieCache = LoadMovieCache();
        _logger.LogInformation("Loaded {MovieCacheCount} Kinoplan movie cache entries", _movieCache.Count);
    }

    public async Task<Dictionary<string, KinoplanRelease>> GetMovies(IEnumerable<string> movieNames, CancellationToken cancellationToken = default)
    {
        var movies = new Dictionary<string, KinoplanRelease>();
        foreach (var movieName in movieNames)
        {
            var movie = await GetMovie(movieName, cancellationToken);
            if (movie != null)
                movies[movieName] = movie;
        }
        return movies;
    }

    private async Task<KinoplanRelease?> GetMovie(string movieName, CancellationToken cancellationToken = default)
    {
        if (_movieCache.TryGetValue(movieName, out var cached))
            return cached.Deserialize<KinoplanRelease>();

        var element = await FetchMovieData(movieName, cancellationToken);
        if (element == null)
            return null;

        _movieCache[movieName] = element.Value;
        SaveMovieCache(cancellationToken);
        return element.Value.Deserialize<KinoplanRelease>();
    }

    private async Task<JsonElement?> FetchMovieData(string movieName, CancellationToken cancellationToken = default)
    {
        var apiToken = Configuration.ApiToken;
        if (string.IsNullOrWhiteSpace(apiToken))
            throw new Exception("Kinoplan API Token is not setup");

        var releaseId = await SearchReleaseId(movieName, apiToken, cancellationToken);
        if (releaseId == null)
        {
            _logger.LogWarning("No Kinoplan release found for {MovieName}", movieName);
            return null;
        }

        return await FetchReleaseDetail(releaseId.Value, apiToken, cancellationToken);
    }

    private async Task<int?> SearchReleaseId(string movieName, string apiToken, CancellationToken cancellationToken)
    {
        var encodedQuery = HttpUtility.UrlEncode(movieName);
        var url = $"{BaseUrl}/api/releases/filter?q={encodedQuery}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-KINOPLAN-TOKEN", apiToken);

        _logger.LogInformation("Searching Kinoplan for {MovieName}...", movieName);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);

        var list = document.RootElement.GetProperty("list");
        if (list.GetArrayLength() == 0)
            return null;

        return list[0].GetProperty("id").GetInt32();
    }

    private async Task<JsonElement?> FetchReleaseDetail(int releaseId, string apiToken, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/api/v2/release/{releaseId}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-KINOPLAN-TOKEN", apiToken);

        _logger.LogInformation("Fetching Kinoplan release {ReleaseId}...", releaseId);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);

        _logger.LogInformation("Fetched Kinoplan release {ReleaseId} successfully", releaseId);
        return document.RootElement.Clone();
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
            _logger.LogError(e, "Failed saving Kinoplan movie cache");
        }
    }
}