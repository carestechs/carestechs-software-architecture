namespace Common.Lib.Tenancy;

/// <summary>Tenant identifiers (adrs/database/database-per-tenant.md): validated
/// value types at the core; facades take primitives at module boundaries
/// (adrs/dotnet/module-facade.md).</summary>
public readonly record struct OrgId(string Value)
{
    public static bool TryParse(string? raw, out OrgId orgId)
    {
        orgId = default;
        if (string.IsNullOrEmpty(raw) || raw.Length > 20 || !raw.All(char.IsAsciiLetterOrDigit))
        {
            return false;
        }
        orgId = new OrgId(raw.ToLowerInvariant());
        return true;
    }

    public override string ToString() => Value;
}

public readonly record struct WorkspaceId(string Value)
{
    public static bool TryParse(string? raw, out WorkspaceId workspaceId)
    {
        workspaceId = default;
        if (string.IsNullOrEmpty(raw) || raw.Length > 20 || !raw.All(char.IsAsciiLetterOrDigit))
        {
            return false;
        }
        workspaceId = new WorkspaceId(raw.ToLowerInvariant());
        return true;
    }

    public override string ToString() => Value;
}
