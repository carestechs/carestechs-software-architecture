namespace Common.Lib.Contracts;

/// <summary>Cross-module async work rides queues (adrs/deployment/queue-based-decoupling.md).
/// The correlation id is REQUIRED on every enqueue (adrs/deployment/correlation-propagation.md).</summary>
public interface IQueueProvider
{
    Task EnqueueAsync<TMessage>(
        string queueName, TMessage message, string correlationId, CancellationToken cancellationToken);
}

/// <summary>Secrets Manager in production, gitignored file in dev
/// (adrs/deployment/aws-secrets-parameters.md).</summary>
public interface ISecretsProvider
{
    Task<string> GetSecretAsync(string name, CancellationToken cancellationToken);
}

/// <summary>SSM Parameter Store in production, gitignored file in dev.</summary>
public interface IParametersProvider
{
    Task<string> GetParameterAsync(string name, CancellationToken cancellationToken);
}
