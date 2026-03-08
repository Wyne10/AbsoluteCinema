using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

public class MovieProvider : IMovieProvider
{
    private const string ApiBaseUrl = "https://api.poiskkino.dev/v1.4/movie/search";
    private readonly HttpClient _httpClient = new();
    
    private readonly IOptionsMonitor<MovieProviderConfiguration> _configurationMonitor;
    private MovieProviderConfiguration Configuration => _configurationMonitor.CurrentValue;
    private readonly ILogger<MovieProvider> _logger;
    
    private readonly string _movieCacheFilePath;
    private readonly Dictionary<string, Movie> _movieCache;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    public MovieProvider(IOptionsMonitor<MovieProviderConfiguration> configurationMonitor, ILogger<MovieProvider> logger)
    {
        _configurationMonitor = configurationMonitor;
        _logger = logger;
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolderPath = Path.Combine(appDataPath, "CinemaControl");
        Directory.CreateDirectory(appFolderPath);
        _movieCacheFilePath = Path.Combine(appFolderPath, "movies.json");
        
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
        return _movieCache.TryGetValue(movieName, out var movie) ? movie : WriteMovieCache(movieName, await FetchMovieData(movieName, cancellationToken));
    }

    private async Task<Movie?> FetchMovieData(string movieName, CancellationToken cancellationToken = default)
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
        var searchResult = JsonSerializer.Deserialize<SearchResponse>(jsonResponse);

        var movieDto = searchResult?.Docs
            .Where(movie => !movie.IsSeries ?? false)
            .OrderByDescending(movie => movie.Year)
            .First();

        if (movieDto == null)
            throw new Exception($"Failed fetching movie {movieName}");

        _logger.LogInformation("Fetched movie {MovieName} successfully", movieName);
        return movieDto;
    }

    private Movie? WriteMovieCache(string movieName, Movie? movie)
    {
        if (movie == null)
            return movie; 
        _movieCache[movieName] = movie;
        SaveMovieCache();
        return movie;
    }
    
    private Dictionary<string, Movie> LoadMovieCache()
    {
        if (!File.Exists(_movieCacheFilePath))
            return new Dictionary<string, Movie>();

        try
        {
            var json = File.ReadAllText(_movieCacheFilePath);
            return JsonSerializer.Deserialize<Dictionary<string, Movie>>(json) ?? new Dictionary<string, Movie>();
        }
        catch
        {
            return new Dictionary<string, Movie>();
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