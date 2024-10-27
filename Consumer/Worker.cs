using System.Reflection;
using Consumer.Consumers;
using Shared.Services;

namespace Consumer;

public class Worker(RabbitMqConnectionService rabbitMqConnectionService, ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly ILogger<Worker> _logger = loggerFactory.CreateLogger<Worker>();

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumers = Assembly.GetExecutingAssembly().GetTypes()
            .Where(x => x is { IsClass: true, IsAbstract: false } && x.IsAssignableFrom(typeof(BaseConsumer<>)));

        foreach (var consumer in consumers)
        {
            try
            {
                var instance = Activator.CreateInstance(consumer, rabbitMqConnectionService, loggerFactory, scopeFactory);

                var executingMethod = consumer.BaseType!.GetMethod("StartConsuming");
                executingMethod!.Invoke(instance, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start consumer {Consumer}", consumer.Name);
            }
        }

        return Task.CompletedTask;
    }
}