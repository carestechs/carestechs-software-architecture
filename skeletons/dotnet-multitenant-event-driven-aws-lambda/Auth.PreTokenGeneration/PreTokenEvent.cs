namespace Auth.PreTokenGeneration;

/// <summary>The V2 trigger event shape this handler consumes — recorded
/// fixtures stand in for Cognito in tests (adrs/api/cognito-authentication.md;
/// the real trigger wiring is a phase-3 concern).</summary>
public sealed record PreTokenEvent(
    string UserName,
    string ClientId,
    Dictionary<string, string> UserAttributes);

public sealed record ClaimsOverride(Dictionary<string, string> ClaimsToAddOrOverride);
