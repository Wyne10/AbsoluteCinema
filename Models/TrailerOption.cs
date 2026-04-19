using AbsoluteCinema.Dtos;

namespace AbsoluteCinema.Models;

public abstract record TrailerOption(string Title);

public record FileTrailerOption(string Title, KinoplanFile File) : TrailerOption(Title);

public record ReleaseTrailerOption(string Title, KinoplanReleaseTrailer Trailer) : TrailerOption(Title);
