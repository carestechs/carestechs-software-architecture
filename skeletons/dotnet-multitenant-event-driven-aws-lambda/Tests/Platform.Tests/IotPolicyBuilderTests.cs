using Notification.Authorizer;
using Xunit;

namespace Platform.Tests;

public class IotPolicyBuilderTests
{
    [Fact]
    public void Policy_IsScopedToTheCallersOwnSubtree()
    {
        var policy = IotPolicyBuilder.Build("acme", "main", "user42", "us-east-1", "123456789012");

        Assert.Equal("Allow", policy.Effect);
        Assert.Contains("arn:aws:iot:us-east-1:123456789012:topicfilter/platform/acme/main/user/user42/#",
            policy.Resources);
        // wildcards appear only BELOW the user segment — never above it
        Assert.DoesNotContain(policy.Resources, r => r.Contains("user/+") || r.Contains("main/#")
            || r.Contains("acme/#") || r.Contains("platform/#"));
    }
}
