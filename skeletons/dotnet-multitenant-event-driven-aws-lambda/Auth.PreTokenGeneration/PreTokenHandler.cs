using Common.Lib.Tenancy;
using Tenancy.Application.Contracts;

namespace Auth.PreTokenGeneration;

/// <summary>Pre-token enrichment (adrs/api/cognito-authentication.md): resolve
/// tenant scope ONCE at issuance so every downstream consumer reads validated
/// claims. Any missing piece falls through to no claims — deny-by-default at
/// the APIs does the rest.</summary>
public sealed class PreTokenHandler(IOrganizationDirectory directory)
{
    public async Task<ClaimsOverride?> HandleAsync(
        PreTokenEvent triggerEvent, CancellationToken cancellationToken)
    {
        if (!triggerEvent.UserAttributes.TryGetValue("custom:oid", out var rawOrg)
            || !OrgId.TryParse(rawOrg, out var org))
        {
            return null;
        }

        var organization = await directory.GetAsync(org, cancellationToken);
        if (organization is null || !organization.Enabled)
        {
            return null;
        }

        return new ClaimsOverride(new Dictionary<string, string>
        {
            ["org"] = organization.OrgId,
            ["workspace"] = organization.DefaultWorkspaceId,
            ["sub"] = triggerEvent.UserName,
        });
    }
}
