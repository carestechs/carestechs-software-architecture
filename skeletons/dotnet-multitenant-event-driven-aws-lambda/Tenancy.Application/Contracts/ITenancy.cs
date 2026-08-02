using Common.Lib.Tenancy;

namespace Tenancy.Application.Contracts;

/// <summary>The tenant directory is the ONE deliberately global dataset — it
/// lives in a global store owned by this module
/// (adrs/database/database-per-tenant.md).</summary>
public sealed record OrganizationRecord(string OrgId, string Name, string DefaultWorkspaceId, bool Enabled);

public interface IOrganizationDirectory
{
    Task PutAsync(OrganizationRecord organization, CancellationToken cancellationToken);
    Task<OrganizationRecord?> GetAsync(OrgId orgId, CancellationToken cancellationToken);
}

public interface ITenantDatabaseProvisioner
{
    /// <summary>Creates the tenant database and replays the FULL migration
    /// history — a new tenant's schema is identical to the oldest tenant's.</summary>
    Task ProvisionAsync(OrgId org, WorkspaceId workspace, CancellationToken cancellationToken);
}
