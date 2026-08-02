using DbUp;

namespace Common.Database;

/// <summary>One migration pipeline for every entry point — the tenant
/// provisioner and the operator CLI run exactly this
/// (adrs/deployment/dbup-migrations.md).</summary>
public static class DbUpMigrator
{
    public static void Migrate(string connectionString)
    {
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DbUpMigrator).Assembly)
            .WithTransactionPerScript()
            .LogToNowhere()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            throw new InvalidOperationException(
                $"DbUp migration failed: {result.Error?.Message}", result.Error);
        }
    }
}
