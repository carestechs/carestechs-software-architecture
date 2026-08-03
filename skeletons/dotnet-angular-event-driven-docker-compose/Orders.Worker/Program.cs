using Common.Providers.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orders.Worker;

// One always-on consumer container per queue-triggered concern — the compose
// substrate's mirror of one-Lambda-per-concern (profile convention).
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

WorkerServices.Configure(builder.Services, builder.Configuration);
builder.Services.AddSingleton<RabbitMqConnectionProvider>();
builder.Services.AddHostedService<OrderPlacedConsumer>();

await builder.Build().RunAsync();
