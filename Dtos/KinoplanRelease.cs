using System.Collections.Generic;
using System.Text.Json.Serialization;
// ReSharper disable All

namespace AbsoluteCinema.Dtos;

public record KinoplanRelease
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("date")]
    public KinoplanReleaseDate? Date { get; init; }

    [JsonPropertyName("age")]
    public string? Age { get; init; }

    [JsonPropertyName("title")]
    public KinoplanTitle? Title { get; init; }

    [JsonPropertyName("formats")]
    public List<string>? Formats { get; init; }

    [JsonPropertyName("distributors")]
    public List<KinoplanDistributor>? Distributors { get; init; }

    [JsonPropertyName("genres")]
    public List<string>? Genres { get; init; }

    [JsonPropertyName("countries")]
    public List<KinoplanCountry>? Countries { get; init; }

    [JsonPropertyName("description")]
    public KinoplanDescription? Description { get; init; }

    [JsonPropertyName("short_description")]
    public KinoplanDescription? ShortDescription { get; init; }

    [JsonPropertyName("laboratories")]
    public KinoplanLaboratories? Laboratories { get; init; }

    [JsonPropertyName("passport")]
    public string? Passport { get; init; }

    [JsonPropertyName("year")]
    public int? Year { get; init; }

    [JsonPropertyName("cover")]
    public string? Cover { get; init; }

    [JsonPropertyName("cover_small")]
    public string? CoverSmall { get; init; }

    [JsonPropertyName("cover_medium")]
    public string? CoverMedium { get; init; }

    [JsonPropertyName("integrated_trailers")]
    public List<KinoplanIntegratedTrailer>? IntegratedTrailers { get; init; }

    [JsonPropertyName("packages")]
    public List<KinoplanPackage>? Packages { get; init; }

    [JsonPropertyName("files")]
    public List<KinoplanFile>? Files { get; init; }

    [JsonPropertyName("release_trailers")]
    public List<KinoplanReleaseTrailer>? ReleaseTrailers { get; init; }

    [JsonPropertyName("cast")]
    public List<string>? Cast { get; init; }

    [JsonPropertyName("kinopoisk_id")]
    public int? KinopoiskId { get; init; }
}

public record KinoplanReleaseDate
{
    [JsonPropertyName("timestamp")]
    public long? Timestamp { get; init; }

    [JsonPropertyName("year")]
    public int? Year { get; init; }

    [JsonPropertyName("week")]
    public int? Week { get; init; }

    [JsonPropertyName("month")]
    public int? Month { get; init; }

    [JsonPropertyName("string")]
    public string? String { get; init; }

    [JsonPropertyName("russia")]
    public KinoplanRegionDate? Russia { get; init; }

    [JsonPropertyName("world")]
    public KinoplanRegionDate? World { get; init; }

    [JsonPropertyName("start_period")]
    public KinoplanStartPeriod? StartPeriod { get; init; }
}

public record KinoplanRegionDate
{
    [JsonPropertyName("string")]
    public string? String { get; init; }

    [JsonPropertyName("preview")]
    public string? Preview { get; init; }

    [JsonPropertyName("start")]
    public string? Start { get; init; }

    [JsonPropertyName("end")]
    public string? End { get; init; }
}

public record KinoplanStartPeriod
{
    [JsonPropertyName("ru")]
    public string? Ru { get; init; }

    [JsonPropertyName("en")]
    public string? En { get; init; }
}

public record KinoplanTitle
{
    [JsonPropertyName("ru")]
    public string? Ru { get; init; }

    [JsonPropertyName("en")]
    public string? En { get; init; }

    [JsonPropertyName("cinemapark")]
    public string? Cinemapark { get; init; }

    [JsonPropertyName("PU")]
    public string? Pu { get; init; }
}

public record KinoplanDistributor
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public KinoplanDistributorName? Name { get; init; }
}

public record KinoplanDistributorName
{
    [JsonPropertyName("short")]
    public string? Short { get; init; }

    [JsonPropertyName("full")]
    public string? Full { get; init; }
}

public record KinoplanCountry
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

public record KinoplanDescription
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("html")]
    public string? Html { get; init; }
}

public record KinoplanLaboratories
{
    [JsonPropertyName("key_lab")]
    public KinoplanLaboratory? KeyLab { get; init; }

    [JsonPropertyName("rep_lab")]
    public KinoplanLaboratory? RepLab { get; init; }

    [JsonPropertyName("disk_lab1")]
    public KinoplanLaboratory? DiskLab1 { get; init; }

    [JsonPropertyName("disk_lab2")]
    public KinoplanLaboratory? DiskLab2 { get; init; }

    [JsonPropertyName("dd_lab1")]
    public KinoplanLaboratory? DdLab1 { get; init; }

    [JsonPropertyName("dd_lab2")]
    public KinoplanLaboratory? DdLab2 { get; init; }
}

public record KinoplanLaboratory
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("short_name")]
    public string? ShortName { get; init; }

    [JsonPropertyName("address")]
    public string? Address { get; init; }

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("emails")]
    public List<string>? Emails { get; init; }
}

public record KinoplanIntegratedTrailer
{
    [JsonPropertyName("trailer_release_id")]
    public int TrailerReleaseId { get; init; }

    [JsonPropertyName("trailer_id")]
    public int TrailerId { get; init; }

    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("duration")]
    public int Duration { get; init; }

    [JsonPropertyName("title")]
    public KinoplanTitle? Title { get; init; }

    [JsonPropertyName("hidden")]
    public int Hidden { get; init; }
}

public record KinoplanPackage
{
    [JsonPropertyName("lang")]
    public string? Lang { get; init; }

    [JsonPropertyName("trailer_duration")]
    public int TrailerDuration { get; init; }

    [JsonPropertyName("film_title")]
    public string? FilmTitle { get; init; }

    [JsonPropertyName("aspect")]
    public string? Aspect { get; init; }

    [JsonPropertyName("title_duration")]
    public int TitleDuration { get; init; }

    [JsonPropertyName("duration")]
    public int Duration { get; init; }

    [JsonPropertyName("cplid")]
    public string? Cplid { get; init; }

    [JsonPropertyName("format")]
    public string? Format { get; init; }

    [JsonPropertyName("resolution")]
    public string? Resolution { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("audio_type")]
    public string? AudioType { get; init; }

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; init; }
}

public record KinoplanFile
{
    [JsonPropertyName("is_target")]
    public int IsTarget { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("preview")]
    public string? Preview { get; init; }

    [JsonPropertyName("ext")]
    public string? Ext { get; init; }

    [JsonPropertyName("size")]
    public long Size { get; init; }

    [JsonPropertyName("is_main")]
    public bool IsMain { get; init; }

    [JsonPropertyName("created")]
    public long Created { get; init; }
}

public record KinoplanReleaseTrailer
{
    [JsonPropertyName("banner")]
    public string? Banner { get; init; }

    [JsonPropertyName("comment")]
    public string? Comment { get; init; }

    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }
}