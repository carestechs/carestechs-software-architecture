using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Common.Lib.Contracts;

namespace Common.Providers.Parameters;

/// <summary>Production: SSM Parameter Store (adrs/deployment/aws-secrets-parameters.md).</summary>
public sealed class ParametersSsmProvider(IAmazonSimpleSystemsManagement client) : IParametersProvider
{
    public async Task<string> GetParameterAsync(string name, CancellationToken cancellationToken)
    {
        var response = await client.GetParameterAsync(
            new GetParameterRequest { Name = name }, cancellationToken);
        return response.Parameter.Value;
    }
}
