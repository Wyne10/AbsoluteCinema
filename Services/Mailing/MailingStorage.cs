using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AbsoluteCinema.Dtos;
using Microsoft.Extensions.Logging;

namespace AbsoluteCinema.Services.Mailing;

public sealed class MailingStorage(ILogger<MailingStorage> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static string FilePath
    {
        get
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolderPath = Path.Combine(appDataPath, "AbsoluteCinema");
            Directory.CreateDirectory(appFolderPath);
            return Path.Combine(appFolderPath, "mailing.json");
        }
    }

    public MailingData Load()
    {
        if (!File.Exists(FilePath))
        {
            logger.LogInformation("Mailing data file not found at {Path}, returning empty data", FilePath);
            return new MailingData();
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<MailingData>(json, JsonOptions) ?? new MailingData();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load mailing data from {Path}", FilePath);
            return new MailingData();
        }
    }

    public void Save(MailingData data)
    {
        try
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(FilePath, json);
            logger.LogInformation("Mailing data saved to {Path}", FilePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save mailing data to {Path}", FilePath);
        }
    }
}