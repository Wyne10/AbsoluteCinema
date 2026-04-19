using System.Collections.Generic;
using AbsoluteCinema.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AbsoluteCinema.Models;

public partial class VkAnnouncementMovie(
    CinemaWebMovie cinemaWebMovie,
    KinoplanRelease kinoplanRelease,
    List<TrailerOption> trailerOptions) : ObservableObject
{
    public CinemaWebMovie CinemaWebMovie { get; } = cinemaWebMovie;
    public string Name => CinemaWebMovie.Name;
    public string? CoverUrl { get; } = kinoplanRelease.CoverMedium ?? kinoplanRelease.Cover;
    public string? ShortDescription { get; } = kinoplanRelease.ShortDescription?.Text;
    public List<TrailerOption> TrailerOptions { get; } = trailerOptions;

    [ObservableProperty]
    private TrailerOption? _selectedTrailer;

    [ObservableProperty]
    private bool _isSelected;
}
