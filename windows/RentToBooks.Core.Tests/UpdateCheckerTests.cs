using RentToBooks.Core;

namespace RentToBooks.Core.Tests;

public class UpdateCheckerTests
{
    [Theory]
    [InlineData("v0.2.0", 0, 2, 0)]
    [InlineData("V0.2.0", 0, 2, 0)]
    [InlineData("0.2.0", 0, 2, 0)]
    [InlineData("v1.0.0", 1, 0, 0)]
    public void TryParseVersion_ParsesTagWithOrWithoutVPrefix(
        string tag, int major, int minor, int build)
    {
        var parsed = UpdateChecker.TryParseVersion(tag, out var version);

        Assert.True(parsed);
        Assert.Equal(new Version(major, minor, build), version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-version")]
    [InlineData("v")]
    public void TryParseVersion_RejectsMalformedTags(string tag)
    {
        var parsed = UpdateChecker.TryParseVersion(tag, out _);

        Assert.False(parsed);
    }

    [Fact]
    public void Version_ComparisonTreatsNewerTagAsGreater()
    {
        UpdateChecker.TryParseVersion("v0.1.0", out var current);
        UpdateChecker.TryParseVersion("v0.2.0", out var latest);

        Assert.True(latest > current);
    }
}
