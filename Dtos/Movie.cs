using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
// ReSharper disable All

namespace AbsoluteCinema.Dtos;

public record SearchResponse
{
    [JsonPropertyName("docs")]
    public required List<Movie> Docs { get; init; }

    [JsonPropertyName("total")]
    public required int Total { get; init; }

    [JsonPropertyName("limit")]
    public required int Limit { get; init; }

    [JsonPropertyName("page")]
    public required int Page { get; init; }

    [JsonPropertyName("pages")]
    public required int Pages { get; init; }
}

public record Movie
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }
    
    [JsonPropertyName("externalId")]
    public ExternalId? ExternalId { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("alternativeName")]
    public string? AlternativeName { get; init; }
    
    [JsonPropertyName("enName")]
    public string? EnglishName { get; init; }
    
    [JsonPropertyName("names")]
    public List<Names>? Names { get; init; }
    
    [JsonPropertyName("type")]
    public string? Type { get; init; }
    
    [JsonPropertyName("year")]
    public int? Year { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("shortDescription")]
    public string? ShortDescription { get; init; }
    
    [JsonPropertyName("movieLength")]
    public int? Length { get; init; } 
    
    [JsonPropertyName("isSeries")]
    public bool? IsSeries { get; init; }  
    
    [JsonPropertyName("ageRating")]
    public int? AgeRating { get; init; } 
        
    [JsonPropertyName("logo")]
    public Logo? Logo { get; init; }
    
    [JsonPropertyName("poster")]
    public Poster? Poster { get; init; }
    
    [JsonPropertyName("backdrop")]
    public Poster? Backdrop { get; init; }

    [JsonPropertyName("rating")]
    public Rating? Rating { get; init; }

    [JsonPropertyName("genres")]
    public List<Genre>? Genres { get; init; }

    [JsonPropertyName("countries")]
    public List<Country>? Countries { get; init; }
    
    public bool IsRussian() => Countries?.Any(c => c.Name == "Россия") ?? false;

    public bool AreChildrenAvailable() => AgeRating <= 6;
}

public record Names
{
    [JsonPropertyName("language")]
    public string? Language { get; init; }
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    [JsonPropertyName("type")]
    public string? Type { get; init; }
}

public record ExternalId
{
    [JsonPropertyName("imdb")]
    public string? Imdb { get; init; }
    [JsonPropertyName("KpHD")]
    public string? KpHd { get; init; }
    [JsonPropertyName("tmdb")]
    public int? Tmdb { get; init; }
}

public record Logo
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }
}

public record Poster
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("previewUrl")]
    public string? PreviewUrl { get; init; }
}

public record Video
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }
    [JsonPropertyName("name")]
    public string? Name { get; init; } 
    [JsonPropertyName("site")]
    public string? Site { get; init; }  
    [JsonPropertyName("size")]
    public double? Size { get; init; } 
    [JsonPropertyName("type")]
    public double? Type { get; init; } 
}

public record Rating
{
    [JsonPropertyName("kp")]
    public double? Kp { get; init; }
    [JsonPropertyName("imdb")]
    public double? Imdb { get; init; }
    [JsonPropertyName("tmdb")]
    public double? Tmdb { get; init; }
    [JsonPropertyName("filmCritics")]
    public double? FilmCritics { get; init; }
    [JsonPropertyName("russianFilmCritics")]
    public double? RussianFilmCritics { get; init; }
    [JsonPropertyName("await")]
    public double? Await { get; init; }
}
    
public record Genre
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}

public record Country
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}