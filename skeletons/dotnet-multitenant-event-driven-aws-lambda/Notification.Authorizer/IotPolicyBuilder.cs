namespace Notification.Authorizer;

/// <summary>IoT custom-authorizer policy (adrs/deployment/iot-mqtt-push.md):
/// the granted policy is scoped to the caller's OWN topic subtree — wildcards
/// only below the user segment; cross-user and cross-tenant subscribes are
/// denied at SUBSCRIBE time by construction. Pure function: JWT claims in,
/// policy out — the IoT wiring itself is a phase-3 concern.</summary>
public sealed record IotPolicy(string Effect, IReadOnlyList<string> Actions, IReadOnlyList<string> Resources);

public static class IotPolicyBuilder
{
    public static IotPolicy Build(string org, string workspace, string userId, string region, string accountId)
    {
        var topic = $"platform/{org}/{workspace}/user/{userId}";
        return new IotPolicy(
            "Allow",
            ["iot:Connect", "iot:Subscribe", "iot:Receive"],
            [
                $"arn:aws:iot:{region}:{accountId}:client/{userId}-*",
                $"arn:aws:iot:{region}:{accountId}:topicfilter/{topic}/#",
                $"arn:aws:iot:{region}:{accountId}:topic/{topic}/*",
            ]);
    }
}
