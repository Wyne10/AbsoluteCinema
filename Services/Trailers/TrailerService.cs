using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AbsoluteCinema.Dtos;
using Microsoft.Extensions.Logging;

namespace AbsoluteCinema.Services.Trailers;

public sealed class TrailerService(string rootPath, string ffmpegPath, ILogger logger) : ITrailerService, IDisposable
{
    private readonly HttpClient _httpClient = new();

    public async Task<string> RenderTrailers(
        IEnumerable<KinoplanFile> files,
        IProgress<TrailerProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var fileList = files.ToList();
        if (fileList.Count == 0)
            throw new InvalidOperationException("No video files selected");

        logger.LogInformation("Downloading {Count} video files...", fileList.Count);
        var downloadDir = Path.Combine(Path.GetTempPath(), "AbsoluteCinema");
        Directory.CreateDirectory(downloadDir);

        var totalBytes = fileList.Sum(f => f.Size);
        long downloadedBytes = 0;
        var sw = Stopwatch.StartNew();

        var localPaths = new List<string>();
        foreach (var file in fileList)
        {
            var localPath = await DownloadFile(file, downloadDir, bytes =>
            {
                downloadedBytes += bytes;
                var fraction = totalBytes > 0 ? (double)downloadedBytes / totalBytes : 0;

                TimeSpan? eta = null;
                if (fraction > 0.01 && sw.Elapsed.TotalSeconds > 1)
                {
                    var remaining = sw.Elapsed.TotalSeconds / fraction * (1 - fraction);
                    eta = TimeSpan.FromSeconds(remaining);
                }

                progress?.Report(new TrailerProgress(fraction, downloadedBytes, totalBytes, eta));
            }, cancellationToken);

            if (localPath is null)
                continue;

            localPaths.Add(localPath);
        }

        progress?.Report(new TrailerProgress(1, totalBytes, totalBytes, TimeSpan.Zero));

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var outputPath = Path.Combine(rootPath, $"trailers_{timestamp}.mp4");
        await ConcatenateVideos(localPaths, outputPath, cancellationToken);

        return outputPath;
    }

    private async Task<string?> DownloadFile(
        KinoplanFile file, string downloadDir,
        Action<long> onBytesRead,
        CancellationToken cancellationToken)
    {
        if (file.Path is null)
        {
            logger.LogWarning("File {Title} has no download path, skipping...", file.Title);
            return null;
        }

        var fileName = $"{file.Id}_{file.Title ?? "trailer"}.{file.Ext ?? "mp4"}";
        var invalidChars = Path.GetInvalidFileNameChars();
        fileName = new string(fileName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        var localPath = Path.Combine(downloadDir, fileName);

        if (File.Exists(localPath))
        {
            logger.LogDebug("File already downloaded: {Path}", localPath);
            onBytesRead(file.Size);
            return localPath;
        }

        logger.LogDebug("Downloading {Title} from {Url}...", file.Title, file.Path);

        using var response = await _httpClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, file.Path),
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = File.Create(localPath);

        var buffer = new byte[81920];
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            onBytesRead(bytesRead);
        }

        logger.LogInformation("Downloaded {Title} to {Path}", file.Title, localPath);
        return localPath;
    }

    private async Task ConcatenateVideos(List<string> videoPaths, string outputPath, CancellationToken cancellationToken)
    {
        var concatListPath = outputPath + ".txt";
        var concatLines = videoPaths.Select(p => $"file '{p}'");
        await File.WriteAllLinesAsync(concatListPath, concatLines, cancellationToken);

        try
        {
            var args = $"-f concat -safe 0 -i \"{concatListPath}\" -c copy \"{outputPath}\"";

            logger.LogInformation("Running ffmpeg: {Path} {Args}", ffmpegPath, args);

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process.Start();
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"ffmpeg failed with exit code {process.ExitCode}: {stderr}");

            logger.LogInformation("Trailers rendered to {Path}", outputPath);
        }
        finally
        {
            File.Delete(concatListPath);
        }
    }
    
    public void Dispose()
    {
        _httpClient.Dispose(); 
    }
}