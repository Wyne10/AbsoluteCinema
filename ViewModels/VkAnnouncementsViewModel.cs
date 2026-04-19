using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AbsoluteCinema.Dtos;
using AbsoluteCinema.Models;
using AbsoluteCinema.Services.Movies;
using AbsoluteCinema.Services.Trailers;
using AbsoluteCinema.Services.Vk;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace AbsoluteCinema.ViewModels;

public partial class VkAnnouncementsViewModel(
    IMovieProvider<KinoplanRelease> kinoplanProvider,
    VkService vkService,
    ILogger<VkAnnouncementsViewModel> logger,
    IHostApplicationLifetime lifetime)
    : ViewModelBase
{
    public ObservableCollection<VkAnnouncementMovie> Movies { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadMoviesCommand))]
    [NotifyCanExecuteChangedFor(nameof(PublishCommand))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PublishCommand))]
    private bool _isPublishing;

    [ObservableProperty]
    private string? _statusText;

    [ObservableProperty]
    private double _publishProgress;

    [RelayCommand(CanExecute = nameof(CanLoadMovies))]
    private async Task LoadMovies()
    {
        try
        {
            IsLoading = true;
            Movies.Clear();

            using var cinemaWeb = new CinemaWebMovieProvider(logger);
            var activeMovies = await cinemaWeb.GetActiveMoviesAsync(lifetime.ApplicationStopping);
            var movieNames = activeMovies.Select(m => m.Name).ToList();

            var kinoplanMovies = await kinoplanProvider.GetMovies(movieNames, lifetime.ApplicationStopping);

            foreach (var cwMovie in activeMovies)
            {
                if (!kinoplanMovies.TryGetValue(cwMovie.Name, out var release))
                    continue;

                var fileTrailers = release.Files?
                    .Where(f => f.Type != null && f.Type.Equals("trailer", StringComparison.OrdinalIgnoreCase))
                    .Where(f => f.Ext?.ToLowerInvariant() is "mp4" or "avi" or "mkv" or "mov")
                    .ToList() ?? [];

                var trailerOptions = fileTrailers
                    .Select(file => new FileTrailerOption(file.Title ?? "Файл трейлера", file)).Cast<TrailerOption>().ToList();

                if (release.ReleaseTrailers is { Count: > 0 })
                {
                    trailerOptions.AddRange(release.ReleaseTrailers
                        .Select(trailer => new ReleaseTrailerOption(trailer.Title ?? "Видео трейлер", trailer)));
                }

                if (trailerOptions.Count == 0) continue;

                var movie = new VkAnnouncementMovie(cwMovie, release, trailerOptions);
                movie.PropertyChanged += (_, _) => PublishCommand.NotifyCanExecuteChanged();
                Movies.Add(movie);
            }

            logger.LogInformation("Loaded {Count} movies for VK announcements", Movies.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load movies for VK");
            var box = MessageBoxManager
                .GetMessageBoxStandard("Ошибка", $"Не удалось загрузить фильмы: {ex.Message}", ButtonEnum.Ok, Icon.Error);
            await box.ShowAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanLoadMovies() => !IsLoading;

    [RelayCommand(CanExecute = nameof(CanPublish))]
    private async Task Publish()
    {
        try
        {
            IsPublishing = true;
            PublishProgress = 0;
            StatusText = "Подготовка публикации...";

            var selectedMovies = Movies
                .Where(m => m is { IsSelected: true, SelectedTrailer: not null })
                .ToList();

            // Build post text
            var message = BuildPostMessage(selectedMovies);

            // Build attachments
            var attachments = new List<string>();
            var totalSteps = selectedMovies.Count;

            for (var i = 0; i < selectedMovies.Count; i++)
            {
                var movie = selectedMovies[i];
                StatusText = $"Загрузка трейлера {i + 1}/{totalSteps}: {movie.Name}";
                PublishProgress = (double)i / totalSteps * 80;

                var attachment = await PrepareAttachment(movie, lifetime.ApplicationStopping);
                if (attachment is not null)
                    attachments.Add(attachment);
            }

            StatusText = "Публикация на стену VK...";
            PublishProgress = 90;

            var attachmentsStr = attachments.Count > 0 ? string.Join(",", attachments) : null;
            var postId = await vkService.WallPostAsync(message, attachmentsStr, lifetime.ApplicationStopping);

            PublishProgress = 100;
            StatusText = $"Опубликовано (пост #{postId})";
            logger.LogInformation("Published VK post {PostId} with {AttachmentCount} attachments", postId, attachments.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish VK post");
            var box = MessageBoxManager
                .GetMessageBoxStandard("Ошибка", $"Не удалось опубликовать: {ex.Message}", ButtonEnum.Ok, Icon.Error);
            await box.ShowAsync();
        }
        finally
        {
            IsPublishing = false;
        }
    }

    private bool CanPublish() =>
        !IsLoading && !IsPublishing && Movies.Any(m => m.IsSelected && m.SelectedTrailer is not null);

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var movie in Movies)
            movie.IsSelected = true;
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var movie in Movies)
            movie.IsSelected = false;
    }

    private static string BuildPostMessage(List<VkAnnouncementMovie> selectedMovies)
    {
        var builder = new StringBuilder();

        foreach (var cw in selectedMovies.Select(movie => movie.CinemaWebMovie))
        {
            builder.AppendLine(
                $"🎬 c {cw.DistributionBegin:d MMMM} - {cw.DistributionEnd:d MMMM}" +
                $" «{cw.Name}», {cw.AgeRestriction}, " +
                $"{string.Join(", ", cw.Formats)}, {cw.Details?.Countries ?? ""}, {cw.Details?.Genres.ToLower() ?? ""}");
        }

        // For single movie, append short description
        if (selectedMovies.Count != 1 || string.IsNullOrWhiteSpace(selectedMovies[0].ShortDescription))
            return builder.ToString().TrimEnd();
        
        builder.AppendLine();
        builder.Append(selectedMovies[0].ShortDescription);

        return builder.ToString().TrimEnd();
    }

    private async Task<string?> PrepareAttachment(VkAnnouncementMovie movie, CancellationToken ct)
    {
        switch (movie.SelectedTrailer)
        {
            case FileTrailerOption { File.Path: not null } fileOption:
            {
                using var trailerService = new TrailerService("", "", logger);
                var localPath = await trailerService.DownloadFileAsync(fileOption.File, ct);
                if (localPath is null) return null;
                return await vkService.UploadVideoFileAsync(localPath, movie.Name, ct);
            }

            case ReleaseTrailerOption releaseOption:
            {
                var videoUrl = VkService.ExtractVideoUrlFromEmbed(releaseOption.Trailer.Code);
                if (videoUrl is not null) return await vkService.SaveExternalVideoAsync(videoUrl, movie.Name, ct);
                logger.LogWarning("Could not extract video URL from embed code for {MovieName}", movie.Name);
                return null;
            }

            default:
                return null;
        }
    }
}
