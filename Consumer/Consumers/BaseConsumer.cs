using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Services;

namespace Consumer.Consumers;

public abstract class BaseConsumer<T> where T : class, new()
{
    protected readonly IModel Channel;
    protected readonly ILogger<T> Logger;
    protected readonly IServiceScopeFactory ScopeFactory;
    protected string QueueName;
    protected string ExchangeName;
    protected string RoutingKey;
    protected int MaxRetryCount = 5; // Default 5 times
    protected int RetryDelayMs = 300_000; // Default 5 minutes

    protected BaseConsumer(RabbitMqConnectionService rabbitMqConnectionService, ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory)
    {
        Channel = rabbitMqConnectionService.GetChannelAsync().Result;
        Logger = loggerFactory.CreateLogger<T>();
        ScopeFactory = scopeFactory;
    }

    protected abstract Task ConsumeAsync(T message);

    protected abstract void SetupConsumer();

    public void StartConsuming()
    {
        try
        {
            SetupConsumer();
            var queueArguments = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", ExchangeName },
                { "x-dead-letter-routing-key", RoutingKey }
            };
            var retryQueueArguments = new Dictionary<string, object>
            {
                { "x-message-ttl", RetryDelayMs },
                { "x-dead-letter-exchange", ExchangeName },
                { "x-dead-letter-routing-key", RoutingKey }
            };

            Channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Direct);
            Channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArguments);
            Channel.QueueDeclare(queue: $"{QueueName}.retry", durable: true, exclusive: false, autoDelete: false, arguments: retryQueueArguments);
            Channel.QueueBind(queue: QueueName, exchange: ExchangeName, routingKey: RoutingKey);
            Channel.QueueBind(queue: $"{QueueName}.retry", exchange: ExchangeName, routingKey: $"{RoutingKey}.retry");

            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += async (_, ea) =>
            {
                string? messageJson = null;
                int currentRetryCount = 0;
                try
                {
                    if (ea.BasicProperties.Headers.TryGetValue("max-retry-count", out object? value))
                    {
                        currentRetryCount = (int)value;
                    }
                    
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    messageJson = json;
                    var message = JsonSerializer.Deserialize<T>(json);
                    if (message is not null)
                    {
                        await ConsumeAsync(message);
                    }

                    Channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    currentRetryCount++;
                    Logger.LogError(ex, "Failed to consume message QueueName: {QueueName} Message: {Message}", QueueName, messageJson);

                    if (currentRetryCount < MaxRetryCount)
                    {
                        Channel.BasicReject(ea.DeliveryTag, false);
                        Channel.BasicPublish(exchange: ExchangeName, routingKey: $"{RoutingKey}.retry", body: ea.Body);
                    }
                    else
                    {
                        // Optional: Save DB for Process Manual or Alert or something
                        Channel.BasicAck(ea.DeliveryTag, false);
                    }
                }
            };
            Channel.BasicConsume(QueueName, false, consumer);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start consuming QueueName: {QueueName}", QueueName);
        }
    }
}