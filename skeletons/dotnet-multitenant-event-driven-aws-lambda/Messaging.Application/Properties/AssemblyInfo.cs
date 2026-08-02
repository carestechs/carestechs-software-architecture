using System.Runtime.CompilerServices;

// The module's internals are shared only with its own Data layer and tests —
// consumers get the facade (adrs/dotnet/module-facade.md).
[assembly: InternalsVisibleTo("Messaging.Data")]
[assembly: InternalsVisibleTo("Messaging.Tests")]
