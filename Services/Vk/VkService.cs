using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AbsoluteCinema.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AbsoluteCinema.Services.Vk;

public sealed partial class VkService(
    IOptionsMonitor<VkConfiguration> configuration,
    ILogger<VkService> logger)
{
    private readonly HttpClient _httpClient = new();
    private const string VkApiBase = "https://api.vk.com/method";
    private const string ApiVersion = "5.199";

    private VkConfiguration Configuration => configuration.CurrentValue;

    public async Task<long> WallPostAsync(string message, string? attachments = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["owner_id"] = $"-{Configuration.GroupId}",
            ["from_group"] = "1",
            ["message"] = message,
            ["access_token"] = Configuration.AccessToken,
            ["v"] = ApiVersion
        };

        if (!string.IsNullOrWhiteSpace(attachments))
            parameters["attachments"] = attachments;

        logger.LogInformation("Posting to VK wall for group {GroupId}...", Configuration.GroupId);

        using var response = await _httpClient.PostAsync(
            $"{VkApiBase}/wall.post",
            new FormUrlEncodedContent(parameters), cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = JsonDocument.Parse(json);

        ThrowIfVkError(doc);

        var postId = doc.RootElement.GetProperty("response").GetProperty("post_id").GetInt64();
        logger.LogInformation("Published VK wall post {PostId}", postId);
        return postId;
    }

    public async Task<string> UploadVideoFileAsync(string filePath, string title, CancellationToken cancellationToken = default)
    {
        var (uploadUrl, ownerId, videoId) = await VideoSaveAsync(title, null, cancellationToken);

        logger.LogInformation("Uploading video file {FilePath} to VK...", filePath);

        using var content = new MultipartFormDataContent();
        await using var fileStream = File.OpenRead(filePath);
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
        content.Add(streamContent, "video_file", Path.GetFileName(filePath));

        using var response = await _httpClient.PostAsync(uploadUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Uploaded video {VideoId} to VK", videoId);
        return $"video{ownerId}_{videoId}";
    }

    public async Task<string> SaveExternalVideoAsync(string url, string title, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving external video {Url} to VK...", url);

        var (uploadUrl, ownerId, videoId) = await VideoSaveAsync(title, url, cancellationToken);

        using var response = await _httpClient.PostAsync(uploadUrl, null, cancellationToken);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Saved external video {VideoId} to VK", videoId);
        return $"video{ownerId}_{videoId}";
    }

    public async Task<string> UploadWallPhotoFromUrlAsync(string imageUrl, CancellationToken ct = default)
    {
        // Step 1: Get upload server
        var serverParams = new Dictionary<string, string>
        {
            ["group_id"] = Configuration.GroupId.ToString(),
            ["access_token"] = Configuration.AccessToken,
            ["v"] = ApiVersion
        };

        using var serverResponse = await _httpClient.PostAsync(
            $"{VkApiBase}/photos.getWallUploadServer",
            new FormUrlEncodedContent(serverParams), ct);
        serverResponse.EnsureSuccessStatusCode();

        var serverJson = await serverResponse.Content.ReadAsStringAsync(ct);
        var serverDoc = JsonDocument.Parse(serverJson);
        ThrowIfVkError(serverDoc);

        var uploadUrl = serverDoc.RootElement
            .GetProperty("response")
            .GetProperty("upload_url")
            .GetString()!;

        // Step 2: Download image and upload to VK
        logger.LogInformation("Downloading image from {Url} for VK upload...", imageUrl);
        var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl, ct);

        using var uploadContent = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(imageContent, "photo", "cover.jpg");

        using var uploadResponse = await _httpClient.PostAsync(uploadUrl, uploadContent, ct);
        uploadResponse.EnsureSuccessStatusCode();

        var uploadJson = await uploadResponse.Content.ReadAsStringAsync(ct);
        var uploadDoc = JsonDocument.Parse(uploadJson);

        // Step 3: Save wall photo
        var saveParams = new Dictionary<string, string>
        {
            ["group_id"] = Configuration.GroupId.ToString(),
            ["photo"] = uploadDoc.RootElement.GetProperty("photo").GetString()!,
            ["server"] = uploadDoc.RootElement.GetProperty("server").GetRawText(),
            ["hash"] = uploadDoc.RootElement.GetProperty("hash").GetString()!,
            ["access_token"] = Configuration.AccessToken,
            ["v"] = ApiVersion
        };

        using var saveResponse = await _httpClient.PostAsync(
            $"{VkApiBase}/photos.saveWallPhoto",
            new FormUrlEncodedContent(saveParams), ct);
        saveResponse.EnsureSuccessStatusCode();

        var saveJson = await saveResponse.Content.ReadAsStringAsync(ct);
        var saveDoc = JsonDocument.Parse(saveJson);
        ThrowIfVkError(saveDoc);

        var photo = saveDoc.RootElement.GetProperty("response").EnumerateArray().First();
        var ownerId = photo.GetProperty("owner_id").GetInt64();
        var photoId = photo.GetProperty("id").GetInt64();

        logger.LogInformation("Uploaded wall photo {PhotoId} to VK", photoId);
        return $"photo{ownerId}_{photoId}";
    }

    private async Task<(string UploadUrl, long OwnerId, long VideoId)> VideoSaveAsync(string title, string? link, CancellationToken ct)
    {
        var parameters = new Dictionary<string, string>
        {
            ["group_id"] = Configuration.GroupId.ToString(),
            ["name"] = title,
            ["access_token"] = Configuration.AccessToken,
            ["v"] = ApiVersion
        };

        if (!string.IsNullOrWhiteSpace(link))
            parameters["link"] = link;

        using var response = await _httpClient.PostAsync(
            $"{VkApiBase}/video.save",
            new FormUrlEncodedContent(parameters), ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);

        ThrowIfVkError(doc);

        var resp = doc.RootElement.GetProperty("response");
        return (
            resp.GetProperty("upload_url").GetString()!,
            resp.GetProperty("owner_id").GetInt64(),
            resp.GetProperty("video_id").GetInt64()
        );
    }

    private static void ThrowIfVkError(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("error", out var error))
            return;

        var errorCode = error.GetProperty("error_code").GetInt32();
        var errorMsg = error.GetProperty("error_msg").GetString();
        throw new InvalidOperationException($"VK API error {errorCode}: {errorMsg}");
    }

    public static string? ExtractVideoUrlFromEmbed(string? embedCode)
    {
        if (string.IsNullOrWhiteSpace(embedCode))
            return null;

        var match = IframeSrcRegex().Match(embedCode);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex("""src=["']([^"']+)["']""")]
    private static partial Regex IframeSrcRegex();
}
