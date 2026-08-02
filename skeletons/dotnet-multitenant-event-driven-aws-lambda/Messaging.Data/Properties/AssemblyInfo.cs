using System.Runtime.CompilerServices;

// Data-layer internals are visible to the module's tests only
// (adrs/dotnet/module-facade.md keeps them hidden from consumers).
[assembly: InternalsVisibleTo("Messaging.Tests")]
