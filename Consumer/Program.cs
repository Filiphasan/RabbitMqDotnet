using Consumer;
using Consumer.Workers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddServices(builder.Configuration);
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<ConsumerWorker>();

var host = builder.Build();
await host.RunAsync();