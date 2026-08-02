using System.Text.Json;
using Common.Lib.Contracts;

namespace Common.Providers.Secrets;

/// <summary>Dev: reads a gitignored .secrets JSON file
/// (adrs/deployment/aws-secrets-parameters.md).</summary>
public sealed class SecretsFileProvider(string path) : ISecretsProvider
{
    private readonly Lazy<Dictionary<string, string>> _values = new(() =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? []);

    public Task<string> GetSecretAsync(string name, CancellationToken cancellationToken) =>
        _values.Value.TryGetValue(name, out var value)
            ? Task.FromResult(value)
            : throw new KeyNotFoundException($"Secret '{name}' not found in {path}.");
}
