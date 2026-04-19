using System.Collections.Generic;
using AbsoluteCinema.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AbsoluteCinema.Models;

public partial class TrailerMovie(string name, string? coverUrl, List<KinoplanFile> videoFiles, string? shortDescription = null) : ObservableObject
{
    public string Name { get; } = name;
    public string? CoverUrl { get; } = coverUrl;
    public string? ShortDescription { get; } = shortDescription;
    public List<KinoplanFile> VideoFiles { get; } = videoFiles;

    [ObservableProperty]
    private KinoplanFile? _selectedVideoFile;
}