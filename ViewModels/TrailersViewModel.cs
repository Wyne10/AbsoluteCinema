using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AbsoluteCinema.Configuration;
using AbsoluteCinema.Dtos;
using AbsoluteCinema.Models;
using AbsoluteCinema.Services.Movies;
using AbsoluteCinema.Services.Trailers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace AbsoluteCinema.ViewModels;

public partial class TrailersViewModel : ViewModelBase
{
    private readonly IOptionsMonitor<TrailerConfiguration> _configuration;
    private readonly IMovieProvider<KinoplanRelease> _kinoplanProvider;
    private readonly ILogger<TrailersViewModel> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public ObservableCollection<TrailerMovie> Movies { get; } = [];
    public ObservableCollection<RenderedTrailer> RenderedTrailers { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadMoviesCommand))]
    [NotifyCanExecuteChangedFor(nameof(RenderTrailersCommand))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RenderTrailersCommand))]
    private bool _isRendering;

    [ObservableProperty]
    private string? _lastOutputPath;

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private string? _downloadStatusText;

    private TrailerConfiguration Configuration => _configuration.CurrentValue;

    public TrailersViewModel(
        IOptionsMonitor<TrailerConfiguration> configuration,
        IMovieProvider<KinoplanRelease> kinoplanProvider,
        ILogger<TrailersViewModel> logger,
        IHostApplicationLifetime lifetime)
    {
        _configuration = configuration;
        _kinoplanProvider = kinoplanProvider;
        _logger = logger;
        _lifetime = lifetime;
        RefreshRenderedTrailers();
    }

    [RelayCommand(CanExecute = nameof(CanLoadMovies))]
    private async Task LoadMovies()
    {
        try
        {
            IsLoading = true;
            Movies.Clear();

            using var cinemaWeb = new CinemaWebMovieProvider(_logger);
            var activeMovies = await cinemaWeb.GetActiveMoviesAsync(_lifetime.ApplicationStopping);
            var movieNames = activeMovies.Select(m => m.Name).ToList();

            var kinoplanMovies = await _kinoplanProvider.GetMovies(movieNames, _lifetime.ApplicationStopping);

            foreach (var cwMovie in activeMovies)
            {
                if (!kinoplanMovies.TryGetValue(cwMovie.Name, out var release))
                    continue;

                var videoFiles = release.Files?
                    .Where(f => f.Type != null && f.Type.Equals("trailer", StringComparison.OrdinalIgnoreCase))
                    .Where(f => f.Ext?.ToLowerInvariant() is "mp4" or "avi" or "mkv" or "mov")
                    .ToList() ?? [];

                if (videoFiles.Count == 0) continue;

                var movie = new TrailerMovie(cwMovie.Name, release.CoverMedium ?? release.Cover, videoFiles);
                movie.PropertyChanged += (_, _) => RenderTrailersCommand.NotifyCanExecuteChanged();
                Movies.Add(movie);
            }

            _logger.LogInformation("Loaded {Count} movies with trailer files", Movies.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load movies");
            var box = MessageBoxManager
                .GetMessageBoxStandard("Ошибка", $"Не удалось загрузить фильмы: {ex.Message}", ButtonEnum.Ok, Icon.Error);
            await box.ShowAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public bool CanLoadMovies() => !IsLoading;

    [RelayCommand(CanExecute = nameof(CanRenderTrailers))]
    private async Task RenderTrailers()
    {
        try
        {
            IsRendering = true;

            var selectedFiles = Movies
                .Where(m => m.SelectedVideoFile is not null)
                .Select(m => m.SelectedVideoFile!)
                .ToList();

            DownloadProgress = 0;
            DownloadStatusText = null;

            var progress = new Progress<TrailerProgress>(p =>
            {
                DownloadProgress = p.Fraction * 100;
                DownloadStatusText = p.StatusText;
            });

            using var trailerService = new TrailerService(Configuration.RootPath, Configuration.FfmpegPath, _logger);
            var outputPath = await trailerService.RenderTrailers(selectedFiles, progress, _lifetime.ApplicationStopping);
            DownloadStatusText = null;
            LastOutputPath = outputPath;
            RefreshRenderedTrailers();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render trailers");
            var box = MessageBoxManager
                .GetMessageBoxStandard("Ошибка", $"Не удалось объединить трейлеры: {ex.Message}", ButtonEnum.Ok, Icon.Error);
            await box.ShowAsync();
        }
        finally
        {
            IsRendering = false;
        }
    }

    public bool CanRenderTrailers() =>
        !IsLoading && !IsRendering && Movies.Any(m => m.SelectedVideoFile is not null);

    private void RefreshRenderedTrailers()
    {
        RenderedTrailers.Clear();

        var rootPath = Configuration.RootPath;
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            return;

        var files = Directory.GetFiles(rootPath, "trailers_*.mp4")
            .OrderByDescending(File.GetCreationTime);

        foreach (var file in files)
        {
            var info = new FileInfo(file);
            RenderedTrailers.Add(new RenderedTrailer(info.Name, file, info.Length, info.CreationTime));
        }
    }

    [RelayCommand]
    private void OpenTrailersFolder()
    {
        var path = Configuration.RootPath;
        if (string.IsNullOrWhiteSpace(path)) return;

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
            desktop.MainWindow.Launcher.LaunchUriAsync(new Uri(path));
    }

    [ObservableProperty]
    private RenderedTrailer? _selectedRenderedTrailer;

    public void OpenSelectedTrailer(Visual visual)
    {
        if (SelectedRenderedTrailer is null) return;
        TopLevel.GetTopLevel(visual)?.Launcher.LaunchUriAsync(new Uri(SelectedRenderedTrailer.FullPath));
    }
}