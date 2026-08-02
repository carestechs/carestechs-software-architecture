using Common.Database;
using Microsoft.Extensions.Logging;

// Operator console: apply the embedded migration history to a target database
// (adrs/deployment/dbup-migrations.md). Same pipeline the provisioner uses.
using var loggerFactory = LoggerFactory.Create(logging => logging.AddJsonConsole());
var logger = loggerFactory.CreateLogger("Common.DatabaseCli");

if (args.Length != 1)
{
    logger.LogError("Usage: Common.DatabaseCli <connection-string>");
    return 2;
}

DbUpMigrator.Migrate(args[0]);
logger.LogInformation("Migrations applied");
return 0;
