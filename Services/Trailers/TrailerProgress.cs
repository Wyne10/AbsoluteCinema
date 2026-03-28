using System;

namespace AbsoluteCinema.Services.Trailers;

public record TrailerProgress(double Fraction, long BytesDownloaded, long TotalBytes, TimeSpan? Eta)
{
    public int Percentage => (int)(Fraction * 100);

    public string StatusText
    {
        get
        {
            var downloaded = FormatBytes(BytesDownloaded);
            var total = FormatBytes(TotalBytes);
            var text = $"{downloaded} / {total} ({Percentage}%)";

            if (Eta is { } eta && eta.TotalSeconds >= 1)
            {
                text += eta.TotalHours >= 1
                    ? $" — ~{eta:h\\:mm\\:ss}"
                    : $" — ~{eta:m\\:ss}";
            }

            return text;
        }
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} ГБ",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} МБ",
        >= 1024 => $"{bytes / 1024.0:F1} КБ",
        _ => $"{bytes} Б"
    };
}