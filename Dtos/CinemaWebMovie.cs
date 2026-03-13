using System.Collections.Generic;

namespace AbsoluteCinema.Dtos;

public record CinemaWebMovie(
    int Id,
    string Name,
    int Duration,
    string AgeRestriction,
    string CertificateNumber,
    string PushkinId,
    List<string> Formats,
    string Description,
    string Country,
    string Genres);