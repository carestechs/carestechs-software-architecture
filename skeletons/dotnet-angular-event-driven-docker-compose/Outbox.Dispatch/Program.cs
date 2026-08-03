using Common.Lib.Contracts;
using Common.Providers.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Orders.Data;
using Outbox.Dispatch;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

var connectionString = builder.Configuration["DATABASE_URL"]
    ?? "Host=localhost;Port=5432;Database=app;Username=postgres;Password=postgres";
builder.Services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton<RabbitMqConnectionProvider>();
builder.Services.AddSingleton<IQueueProvider, RabbitMqQueueProvider>();
builder.Services.AddScoped<OutboxDispatcher>();
builder.Services.AddHostedService<OutboxDispatchService>();

await builder.Build().RunAsync();
