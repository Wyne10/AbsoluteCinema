using System;
using System.Collections.Generic;

namespace AbsoluteCinema.Dtos;

public record CinemaWebMovie(
    int Id,
    string Name,
    int Duration,
    string AgeRestriction,
    string CertificateNumber,
    List<string> Formats,
    DateOnly? DistributionBegin,
    DateOnly? DistributionEnd)
{
    public CinemaWebMovieDetails? Details { get; init; }
}

public record CinemaWebMovieDetails(
    string PushkinId,
    string Description,
    string Country,
    string Genres);