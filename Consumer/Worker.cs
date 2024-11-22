using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Consumer;

public class Worker(IConfiguration configuration, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionFactory = new ConnectionFactory
        {
            HostName = configuration["Settings:RabbitMq:Host"]!,
            Port = Convert.ToInt32(configuration["Settings:RabbitMq:Port"]!),
            UserName = configuration["Settings:RabbitMq:User"]!,
            Password = configuration["Settings:RabbitMq:Password"]!,
            AutomaticRecoveryEnabled = true,
            ClientProvidedName = configuration["Settings:RabbitMq:ConnectionName"]!
        };

        await using var connection = await connectionFactory.CreateConnectionAsync(configuration["Settings:RabbitMq:ConnectionName"]!, stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: "basic.with.bad.way.send.queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                { "x-max-priority", 10 }
            }, cancellationToken: stoppingToken
        );
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, false, cancellationToken: stoppingToken);
        
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            // Simulate work
            await Task.Delay(1000, stoppingToken);
            logger.LogInformation("Message received: {Message}", message);

            await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: stoppingToken);
        };
        await channel.BasicConsumeAsync(queue: "basic.with.bad.way.send.queue", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        
        logger.LogInformation("Basic with bad way Consumer started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}