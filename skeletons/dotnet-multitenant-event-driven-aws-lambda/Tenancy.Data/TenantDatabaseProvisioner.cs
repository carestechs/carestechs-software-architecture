using Common.Database;
using Common.Lib.Tenancy;
using Npgsql;
using Tenancy.Application.Contracts;

namespace Tenancy.Data;

/// <summary>Creates the tenant database and replays the embedded DbUp history —
/// the same pipeline the operator CLI runs (adrs/deployment/dbup-migrations.md,
/// adrs/database/database-per-tenant.md).</summary>
public sealed class TenantDatabaseProvisioner(
    string host, int port, string user, string password) : ITenantDatabaseProvisioner
{
    public async Task ProvisionAsync(OrgId org, WorkspaceId workspace, CancellationToken cancellationToken)
    {
        var databaseName = TenantDbConnectionBuilder.DatabaseName(org, workspace);

        var master = $"Host={host};Port={port};Database=postgres;Username={user};Password={password}";
        await using (var connection = new NpgsqlConnection(master))
        {
            await connection.OpenAsync(cancellationToken);
            await using var exists = new NpgsqlCommand(
                "SELECT 1 FROM pg_database WHERE datname = @name", connection);
            exists.Parameters.AddWithValue("name", databaseName);
            if (await exists.ExecuteScalarAsync(cancellationToken) is null)
            {
                // identifiers are validated [a-z0-9]{1,20}; CREATE DATABASE cannot
                // be parameterized, so the name is built only from those values
                await using var create = new NpgsqlCommand(
                    $"CREATE DATABASE {databaseName}", connection);
                await create.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        DbUpMigrator.Migrate(TenantDbConnectionBuilder.BuildConnectionString(
            host, port, user, password, org, workspace));
    }
}
