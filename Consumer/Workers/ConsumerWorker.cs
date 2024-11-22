using System.Reflection;
using Consumer.Consumers.Abstract;
using Shared.Services;

namespace Consumer.Workers;

public class ConsumerWorker(RabbitMqConnectionService rabbitMqConnectionService, ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly ILogger<Worker> _logger = loggerFactory.CreateLogger<Worker>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return;
        var consumers = Assembly.GetExecutingAssembly().GetTypes()
            .Where(x => x is { IsClass: true, IsPublic: true, IsAbstract: false, Namespace: "Consumer.Consumers" })
            .ToList();

        foreach (var consumer in consumers)
        {
            try
            {
                if (Activator.CreateInstance(consumer, rabbitMqConnectionService, loggerFactory, scopeFactory) is not IBaseConsumer instance)
                {
                    _logger.LogInformation("Failed to create instance of consumer {Consumer}", consumer.Name);
                    continue;
                }

                await instance.StartConsumingAsync(stoppingToken);
                _logger.LogInformation("Started consumer {Consumer}", consumer.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start consumer {Consumer}", consumer.Name);
            }
        }
    }
}