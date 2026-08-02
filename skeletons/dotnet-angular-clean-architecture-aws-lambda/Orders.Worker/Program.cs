using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Common.Providers.Queue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orders.Worker;
using System.Net.Http.Json;

// Dual-mode worker (profile convention): the Lambda runtime bootstrap in
// production; a polling loop against the local queue server in development.
if (Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME") is not null)
{
    var function = new Function();
    var handler = (SQSEvent sqsEvent, ILambdaContext context) => function.HandleAsync(sqsEvent, context);
    await LambdaBootstrapBuilder.Create(handler, new DefaultLambdaJsonSerializer())
        .Build()
        .RunAsync();
    return;
}

var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
var services = WorkerServices.Build(configuration);
var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Orders.Worker");
using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:9324") };
logger.LogInformation("Dev worker polling the local queue server (Ctrl+C to stop)");

using var cancellation = new CancellationTokenSource();
using var sigint = System.Runtime.InteropServices.PosixSignalRegistration.Create(
    System.Runtime.InteropServices.PosixSignal.SIGINT, context =>
    {
        context.Cancel = true;
        cancellation.Cancel();
    });

while (!cancellation.IsCancellationRequested)
{
    var response = await httpClient.GetAsync(
        "/queues/order-placed-queue/messages", cancellation.Token);
    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
    {
        await Task.Delay(1000, cancellation.Token);
        continue;
    }

    var envelope = await response.Content.ReadFromJsonAsync<QueueEnvelope>(cancellation.Token);
    if (envelope is null)
    {
        continue;
    }

    using var scope = services.CreateScope();
    var processor = scope.ServiceProvider.GetRequiredService<OrderPlacedProcessor>();
    await processor.ProcessAsync(envelope.Payload, envelope.CorrelationId, cancellation.Token);
}
