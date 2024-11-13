using System.Text;
using System.Text.Json;
using Consumer.Consumers.Abstract;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Services;

namespace Consumer.Consumers;

public abstract class BaseConsumer<T> : IBaseConsumer where T : class, new()
{
    protected readonly IModel Channel;
    protected readonly ILogger<T> Logger;
    protected readonly IServiceScopeFactory ScopeFactory;
    protected string QueueName = string.Empty; // Main Queue
    protected string ExchangeName = string.Empty; // For Retry
    protected bool UseRetry = true; // For Retry Message when fail
    protected int MaxRetryCount = 5; // Default 5 times
    protected int RetryDelayMs = 300_000; // Default 5 minutes

    private readonly Dictionary<string, object> _delayedExchangeArguments;

    protected BaseConsumer(RabbitMqConnectionService rabbitMqConnectionService, ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory)
    {
        Channel = rabbitMqConnectionService.GetChannelAsync().Result;
        Logger = loggerFactory.CreateLogger<T>();
        ScopeFactory = scopeFactory;

        _delayedExchangeArguments = new Dictionary<string, object>
        {
            { "x-delayed-type", "direct" }
        };
    }

    protected abstract Task ConsumeAsync(T message);

    protected abstract void SetupConsumer();

    public void StartConsuming()
    {
        try
        {
            SetupConsumer();

            Channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            if (UseRetry)
            {
                Channel.ExchangeDeclare(exchange: $"{ExchangeName}.delayed.retry", type: "x-delayed-message", durable: true, arguments: _delayedExchangeArguments);
                Channel.QueueBind(queue: QueueName, exchange: $"{ExchangeName}.delayed.retry", routingKey: "");
            }

            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += async (_, ea) =>
            {
                string? messageJson = null;
                try
                {
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
                    Logger.LogError(ex, "Failed to consume message QueueName: {QueueName} UseRetry: {UseRetry} Message: {Message}", QueueName, UseRetry, messageJson);
                    RetryOrAckMessage(ea, ea.Body.ToArray());
                }
            };
            Channel.BasicConsume(QueueName, false, consumer);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start consuming QueueName: {QueueName}", QueueName);
        }
    }

    private void RetryOrAckMessage(BasicDeliverEventArgs ea, byte[] body, int? delayMs = null)
    {
        try
        {
            int currentRetryCount = 0;
            Channel.BasicAck(ea.DeliveryTag, false);
            if (ea.BasicProperties.Headers.TryGetValue("x-retry-count", out object? value))
            {
                currentRetryCount = (int)value;
            }

            if (!UseRetry)
            {
                return;
            }

            if (currentRetryCount > MaxRetryCount)
            {
                Logger.LogInformation("Max retry count exceeded for message QueueName: {QueueName} Message: {Message}", QueueName, Encoding.UTF8.GetString(body));
                return;
            }

            delayMs ??= RetryDelayMs;

            var properties = Channel.CreateBasicProperties();
            properties.Headers.Add("x-retry-count", currentRetryCount + 1);
            properties.Headers.Add("x-delay", delayMs);
            Channel.BasicPublish($"{ExchangeName}.delayed.retry", "", basicProperties: properties, body: body);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to retry message QueueName: {QueueName}", QueueName);
        }
    }
}