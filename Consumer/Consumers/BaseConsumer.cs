using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using Consumer.Consumers.Abstract;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Services;

namespace Consumer.Consumers;

public abstract class BaseConsumer<T> : IBaseConsumer where T : class, new()
{
    protected IChannel Channel = null!;
    protected readonly ILogger<T> Logger;
    protected readonly IServiceScopeFactory ScopeFactory;
    protected string QueueName = string.Empty; // Main Queue
    protected string ExchangeName = string.Empty; // For Retry
    protected bool UseRetry = true; // For Retry Message when fail
    protected int MaxRetryCount = 5; // Default 5 times
    protected int RetryDelayMs = 300_000; // Default 5 minutes
    
    private readonly RabbitMqConnectionService _rabbitMqConnectionService;
    private readonly Dictionary<string, object?> _delayedExchangeArguments;

    protected BaseConsumer(RabbitMqConnectionService rabbitMqConnectionService, ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory)
    {
        Logger = loggerFactory.CreateLogger<T>();
        ScopeFactory = scopeFactory;
        _rabbitMqConnectionService = rabbitMqConnectionService;

        _delayedExchangeArguments = new Dictionary<string, object?>
        {
            { "x-delayed-type", "direct" }
        };
        
        InitializeChannel();
    }

    private void InitializeChannel()
    {
        var channel = _rabbitMqConnectionService.GetChannelAsync().GetAwaiter().GetResult();
        channel.ChannelShutdownAsync += (_, args) =>
        {
            Logger.LogInformation("RabbitMQ channel shutdown. QueueName: {QueueName} Reason: {Reason}", QueueName, args.ReplyText);
            return Task.CompletedTask;
        };
        Channel = channel;
    }

    protected abstract Task ConsumeAsync(T message);

    protected abstract void SetupConsumer();

    public async Task StartConsumingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            SetupConsumer();

            await Channel.QueueDeclareAsync(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken);
            if (UseRetry)
            {
                await Channel.ExchangeDeclareAsync(exchange: $"{ExchangeName}.delayed.retry", type: "x-delayed-message", durable: true, arguments: _delayedExchangeArguments, cancellationToken: cancellationToken);
                await Channel.QueueBindAsync(queue: QueueName, exchange: $"{ExchangeName}.delayed.retry", routingKey: "", cancellationToken: cancellationToken);
            }
            await Channel.BasicQosAsync(0, 1, false, cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(Channel);
            consumer.ShutdownAsync += (_, args) =>
            {
                Logger.LogInformation("RabbitMQ consumer shutdown. QueueName: {QueueName} Reason: {Reason}", QueueName, args.ReplyText);
                return Task.CompletedTask;
            };
            consumer.ReceivedAsync += async (_, ea) =>
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

                    await Channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to consume message QueueName: {QueueName} UseRetry: {UseRetry} Message: {Message}", QueueName, UseRetry, messageJson);
                    await RetryOrAckMessage(ea, ea.Body.ToArray(), cancellationToken: cancellationToken);
                }
            };
            await Channel.BasicConsumeAsync(QueueName, false, consumer, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start consuming QueueName: {QueueName}", QueueName);
        }
    }

    private async Task RetryOrAckMessage(BasicDeliverEventArgs ea, byte[] body, int? delayMs = null, CancellationToken cancellationToken = default)
    {
        try
        {
            int currentRetryCount = 0;
            await Channel.BasicRejectAsync(ea.DeliveryTag, false, cancellationToken);
            if (ea.BasicProperties.Headers?.TryGetValue("x-retry-count", out object? value) ?? false)
            {
                currentRetryCount = (int)value!;
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
            var properties = new BasicProperties
            {
                Headers = ea.BasicProperties.Headers ?? new Dictionary<string, object?>()
            };
            properties.Headers.Add("x-retry-count", currentRetryCount + 1);
            properties.Headers.Add("x-delay", delayMs);
            await Channel.BasicPublishAsync($"{ExchangeName}.delayed.retry", "", true, properties, body, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to retry or ack message UseRetry: {UseRetry} QueueName: {QueueName}", UseRetry, QueueName);
        }
    }
}