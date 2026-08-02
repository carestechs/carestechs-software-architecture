namespace Common.Lib.Tenancy;

/// <summary>Builds a tenant database connection from validated identifiers —
/// application code never holds a global tenant connection string
/// (adrs/database/database-per-tenant.md).</summary>
public static class TenantDbConnectionBuilder
{
    public static string DatabaseName(OrgId org, WorkspaceId workspace) =>
        $"tenant_{org.Value}_{workspace.Value}"; // identifiers are validated [a-z0-9]{1,20}

    public static string BuildConnectionString(
        string host, int port, string user, string password, OrgId org, WorkspaceId workspace) =>
        $"Host={host};Port={port};Database={DatabaseName(org, workspace)};Username={user};Password={password}";
}
