using TheUnlocker.Registry.Worker;
using TheUnlocker.Registry.Server.Jobs;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IRegistryJobQueue>(_ =>
    string.IsNullOrWhiteSpace(builder.Configuration["Redis:ConnectionString"])
        ? new InMemoryJobQueue()
        : new RedisJobQueue(builder.Configuration["Redis:ConnectionString"]!));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
