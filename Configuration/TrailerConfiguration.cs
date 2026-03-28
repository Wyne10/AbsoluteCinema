namespace AbsoluteCinema.Configuration;

public sealed class TrailerConfiguration
{
    public required string RootPath { get; set; }
    public required string FfmpegPath { get; set; } = "ffmpeg";
}