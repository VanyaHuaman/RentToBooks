namespace RentToBooks.Core;

public sealed record UpdateCheckResult(bool IsUpdateAvailable, Version? LatestVersion, string? ReleaseUrl)
{
    public static UpdateCheckResult NoUpdate { get; } = new(false, null, null);
}
