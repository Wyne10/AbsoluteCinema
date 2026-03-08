using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AbsoluteCinema.Services.Movies;

public partial class CertificateProvider(ILogger logger, string baseUrl = "http://192.168.3.150") : CinemaWebAccessor(baseUrl)
{
    public async Task<Dictionary<string, string>> GetCertificatesAsync(IEnumerable<string> movieNames)
    {
        var movieSet = movieNames.ToHashSet();
        var certificates = new Dictionary<string, string>();
        logger.LogInformation("Fetching certificates for {MovieCount} movies", movieSet.Count);
        
        if (!IsAuthenticated)
        {
            logger.LogDebug("Not authenticated, trying to login..."); 
            if (await LoginAsync())
                logger.LogDebug("Authentication successful");
        }

        // Fetch "В прокате"
        logger.LogTrace("Parsing active certificates...");
        var activePage = await HttpClient.GetStringAsync("/CinemaWeb/Movie");
        ParseTable(activePage, certificates);

        // Fetch "Архивные"
        logger.LogTrace("Parsing archived certificates...");
        var response = await HttpClient.PostAsync("/CinemaWeb/Movie/IndexSetFilter",
            new FormUrlEncodedContent([new KeyValuePair<string, string>("movieFilter", "InArchive")]));
        var archivedPage = await response.Content.ReadAsStringAsync();
        ParseTable(archivedPage, certificates);

        var result = certificates
            .Where(kv => movieSet.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        logger.LogInformation("Fetched {Count} certificates", result.Count);
        return result;
    }

    private static void ParseTable(string html, Dictionary<string, string> certificates)
    {
        // Find movie table (contains "Прокатчик" header)
        foreach (Match table in TableRegex().Matches(html))
        {
            if (!table.Value.Contains("Прокатчик")) continue;

            var rows = RowRegex().Matches(table.Value).Skip(1); // skip header
            foreach (var row in rows)
            {
                var cells = CellRegex().Matches(row.Value);
                if (cells.Count < 8) continue;

                var name = StripTags(cells[0].Groups[1].Value);
                var cert = StripTags(cells[7].Groups[1].Value);

                if (!string.IsNullOrWhiteSpace(name))
                    certificates.TryAdd(name, cert);
            }
        }
    }

    private static string StripTags(string html) =>
        TagRegex().Replace(html, "").Replace("&nbsp;", " ").Trim();

    [GeneratedRegex("<table[^>]*>.*?</table>", RegexOptions.Singleline)]
    private static partial Regex TableRegex();

    [GeneratedRegex("<tr[^>]*>.*?</tr>", RegexOptions.Singleline)]
    private static partial Regex RowRegex();

    [GeneratedRegex("<td[^>]*>(.*?)</td>", RegexOptions.Singleline)]
    private static partial Regex CellRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();
}
