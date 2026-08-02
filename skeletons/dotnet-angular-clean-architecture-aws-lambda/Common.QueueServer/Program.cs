using System.Collections.Concurrent;
using Common.Providers.Queue;

// Lightweight HTTP queue for local development (adrs/deployment/queue-based-decoupling.md):
// dev workers poll it exactly like they would poll SQS in production.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var queues = new ConcurrentDictionary<string, ConcurrentQueue<QueueEnvelope>>();

app.MapPost("/queues/{name}/messages", (string name, QueueEnvelope envelope) =>
{
    queues.GetOrAdd(name, _ => new ConcurrentQueue<QueueEnvelope>()).Enqueue(envelope);
    return Results.Accepted();
});

app.MapGet("/queues/{name}/messages", (string name) =>
    queues.TryGetValue(name, out var queue) && queue.TryDequeue(out var envelope)
        ? Results.Ok(envelope)
        : Results.NoContent());

await app.RunAsync("http://localhost:9324");
