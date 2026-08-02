using Common.Lib.Tenancy;
using Xunit;

namespace Platform.Tests;

public class IdentifierTests
{
    [Theory]
    [InlineData("acme", true)]
    [InlineData("ACME", true)] // normalized to lowercase
    [InlineData("", false)]
    [InlineData("has-dash", false)]
    [InlineData("way_too_long_for_the_limit", false)]
    [InlineData("drop table", false)]
    public void OrgId_ValidatesStrictly(string raw, bool valid)
    {
        Assert.Equal(valid, OrgId.TryParse(raw, out _));
    }
}
