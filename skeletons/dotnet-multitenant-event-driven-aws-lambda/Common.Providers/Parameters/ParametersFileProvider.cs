using System.Text.Json;
using Common.Lib.Contracts;

namespace Common.Providers.Parameters;

/// <summary>Dev: reads a gitignored .parameters JSON file.</summary>
public sealed class ParametersFileProvider(string path) : IParametersProvider
{
    private readonly Lazy<Dictionary<string, string>> _values = new(() =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? []);

    public Task<string> GetParameterAsync(string name, CancellationToken cancellationToken) =>
        _values.Value.TryGetValue(name, out var value)
            ? Task.FromResult(value)
            : throw new KeyNotFoundException($"Parameter '{name}' not found in {path}.");
}
