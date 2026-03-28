using System;

namespace AbsoluteCinema.Models;

public record RenderedTrailer(string Name, string FullPath, long SizeBytes, DateTime CreatedAt)
{
    public string SizeFormatted => SizeBytes switch
    {
        >= 1_073_741_824 => $"{SizeBytes / 1_073_741_824.0:F1} ГБ",
        >= 1_048_576 => $"{SizeBytes / 1_048_576.0:F1} МБ",
        >= 1024 => $"{SizeBytes / 1024.0:F1} КБ",
        _ => $"{SizeBytes} Б"
    };

    public string CreatedAtFormatted => CreatedAt.ToString("dd.MM.yyyy HH:mm");
}