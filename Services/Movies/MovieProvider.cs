using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
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
    private readonly Dictionary<string, Dtos.Movie> _movieCache;

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
    
    public async Task<Dictionary<string, Dtos.Movie>> GetMovies(IEnumerable<string> movieNames)
    {
        var movies = new Dictionary<string, Dtos.Movie>();
        foreach (var movieName in movieNames)
        {
            var movie = await GetMovie(movieName);
            if (movie != null)
                movies[movieName] = movie;
        }
        return movies;
    }

    private async Task<Dtos.Movie?> GetMovie(string movieName)
    {
        return _movieCache.TryGetValue(movieName, out var movie) ? movie : WriteMovieCache(movieName, await FetchMovieData(movieName));
    }

    private async Task<Dtos.Movie?> FetchMovieData(string movieName)
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
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
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

    private Dtos.Movie? WriteMovieCache(string movieName, Dtos.Movie? movie)
    {
        if (movie == null)
            return movie; 
        _movieCache[movieName] = movie;
        SaveMovieCache();
        return movie;
    }
    
    private Dictionary<string, Dtos.Movie> LoadMovieCache()
    {
        if (!File.Exists(_movieCacheFilePath))
            return new Dictionary<string, Dtos.Movie>();

        try
        {
            var json = File.ReadAllText(_movieCacheFilePath);
            return JsonSerializer.Deserialize<Dictionary<string, Dtos.Movie>>(json) ?? new Dictionary<string, Dtos.Movie>();
        }
        catch
        {
            return new Dictionary<string, Dtos.Movie>();
        }
    }

    private async void SaveMovieCache()
    {
        try
        {
            var json = JsonSerializer.Serialize(_movieCache, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_movieCacheFilePath, json);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed saving Poiskkino movie cache");
        }
    }
}