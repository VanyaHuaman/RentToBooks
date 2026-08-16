using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RentToBooks.Core;

/// <summary>
/// Checks GitHub's public releases API for a newer version. This only notifies the user —
/// it never downloads or replaces anything, so it carries none of the trust/signing
/// implications a full auto-updater would.
/// </summary>
public static class UpdateChecker
{
    private const string RepoOwner = "VanyaHuaman";
    private const string RepoName = "RentToBooks";
    private const string UserAgent = "RentToBooks-UpdateChecker";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    private static readonly Uri LatestReleaseApiUri =
        new($"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest");

    public static async Task<UpdateCheckResult> CheckForUpdateAsync(
        Version currentVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new HttpClient { Timeout = RequestTimeout };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            using var response = await client.GetAsync(LatestReleaseApiUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return UpdateCheckResult.NoUpdate;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize<GitHubReleaseResponse>(json);

            if (string.IsNullOrWhiteSpace(payload?.TagName) || string.IsNullOrWhiteSpace(payload?.HtmlUrl))
            {
                return UpdateCheckResult.NoUpdate;
            }

            if (!TryParseVersion(payload.TagName, out var latestVersion))
            {
                return UpdateCheckResult.NoUpdate;
            }

            return latestVersion > currentVersion
                ? new UpdateCheckResult(true, latestVersion, payload.HtmlUrl)
                : UpdateCheckResult.NoUpdate;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return UpdateCheckResult.NoUpdate;
        }
    }

    /// <summary>Parses a release tag like "v0.2.0" or "0.2.0" into a comparable Version.</summary>
    public static bool TryParseVersion(string tagName, out Version version)
    {
        var text = tagName.StartsWith('v') || tagName.StartsWith('V') ? tagName[1..] : tagName;
        return Version.TryParse(text, out version!);
    }

    private sealed record GitHubReleaseResponse(
        [property: JsonPropertyName("tag_name")] string? TagName,
        [property: JsonPropertyName("html_url")] string? HtmlUrl);
}
