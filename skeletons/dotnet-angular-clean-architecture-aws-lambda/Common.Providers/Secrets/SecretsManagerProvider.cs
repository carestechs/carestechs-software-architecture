using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Common.Lib.Contracts;

namespace Common.Providers.Secrets;

/// <summary>Production: AWS Secrets Manager (adrs/deployment/aws-secrets-parameters.md).</summary>
public sealed class SecretsManagerProvider(IAmazonSecretsManager client) : ISecretsProvider
{
    public async Task<string> GetSecretAsync(string name, CancellationToken cancellationToken)
    {
        var response = await client.GetSecretValueAsync(
            new GetSecretValueRequest { SecretId = name }, cancellationToken);
        return response.SecretString;
    }
}
