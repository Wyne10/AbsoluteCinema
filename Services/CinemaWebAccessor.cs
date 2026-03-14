using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AbsoluteCinema.Services;

public abstract partial class CinemaWebAccessor : IDisposable
{
    protected HttpClient HttpClient { get; }
    protected string BaseUrl { get; }
    protected bool IsAuthenticated { get; set; }
    
    protected CinemaWebAccessor(string baseUrl = "http://192.168.3.150")
    {
        BaseUrl = baseUrl.TrimEnd('/');
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = true,
            UseCookies = true
        };
        HttpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
    
    protected async Task<bool> LoginAsync(string username = "Администратор", string password = "", CancellationToken cancellationToken = default)
    {
        var loginPage = await HttpClient.GetStringAsync("/CinemaWeb/Account/Login", cancellationToken);

        var tokenMatch = ForgeryTokenRegex().Match(loginPage);

        if (!tokenMatch.Success)
            throw new Exception("Anti-forgery token not found");

        var loginData = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("__RequestVerificationToken", tokenMatch.Groups[1].Value),
            new KeyValuePair<string, string>("UserName", username),
            new KeyValuePair<string, string>("Password", password),
            new KeyValuePair<string, string>("RememberMe", "false")
        ]);

        var response = await HttpClient.PostAsync("/CinemaWeb/Account/Login", loginData, cancellationToken);
        IsAuthenticated = !response.RequestMessage?.RequestUri?.ToString().Contains("Account/Login") ?? false;

        return IsAuthenticated ? true : throw new Exception("Authentication failed");
    }
    
    protected async Task EnsureAuthenticatedAsync(ILogger logger, CancellationToken cancellationToken)
    {
        if (IsAuthenticated) return;

        logger.LogDebug("Not authenticated, trying to login...");
        if (await LoginAsync(cancellationToken: cancellationToken))
            logger.LogDebug("Authentication successful");
    }
    
    public void Dispose()
    {
        HttpClient.Dispose(); 
        GC.SuppressFinalize(this);
    }
    
    [GeneratedRegex(@"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex ForgeryTokenRegex();
}