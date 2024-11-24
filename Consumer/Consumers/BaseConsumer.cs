using System.Text;
using System.Text.Json;
using Consumer.Consumers.Abstract;
using Consumer.Consumers.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Services;

namespace Consumer.Consumers;

public abstract class BaseConsumer<T> : IBaseConsumer where T : class, new()
{
    protected IChannel Channel = null!;
    protected readonly ILogger<T> Logger;
    protected readonly IServiceScopeFactory ScopeFactory;
    protected readonly ConsumerQueueInfoModel QueueInfo = ConsumerQueueInfoModel.Default; // Main Queue Info
    protected readonly ConsumerExchangeInfoModel ExchangeInfo = ConsumerExchangeInfoModel.Default; // For Exchange Bind
    protected bool UseRetry = true; // For Retry Message when consuming fail
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
            Logger.LogInformation("RabbitMQ channel shutdown. QueueName: {QueueName} Reason: {Reason}", QueueInfo.Name, args.ReplyText);
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

            await Channel.QueueDeclareAsync(QueueInfo.Name, QueueInfo.Durable, QueueInfo.Exclusive, QueueInfo.AutoDelete, QueueInfo.Arguments, cancellationToken: cancellationToken);
            if (!string.IsNullOrEmpty(ExchangeInfo.Name))
            {
                await Channel.ExchangeDeclareAsync(exchange: ExchangeInfo.Name, type: ExchangeInfo.ExchangeType, durable: ExchangeInfo.Durable, arguments: ExchangeInfo.Arguments, cancellationToken: cancellationToken);
                await Channel.QueueBindAsync(queue: QueueInfo.Name, exchange: ExchangeInfo.Name, routingKey: ExchangeInfo.RoutingKey, cancellationToken: cancellationToken);
            }

            if (UseRetry)
            {
                var delayedExchangeName = $"{QueueInfo.Name}.delayed.retry";
                await Channel.ExchangeDeclareAsync(exchange: delayedExchangeName, type: "x-delayed-message", durable: true, arguments: _delayedExchangeArguments, cancellationToken: cancellationToken);
                await Channel.QueueBindAsync(queue: QueueInfo.Name, exchange: delayedExchangeName, routingKey: "", cancellationToken: cancellationToken);
            }

            await Channel.BasicQosAsync(0, 1, false, cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(Channel);
            consumer.ShutdownAsync += (_, args) =>
            {
                Logger.LogInformation("RabbitMQ consumer shutdown. QueueName: {QueueName} Reason: {Reason}", QueueInfo.Name, args.ReplyText);
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
                    Logger.LogError(ex, "Failed to consume message QueueName: {QueueName} UseRetry: {UseRetry} Message: {Message}", QueueInfo.Name, UseRetry, messageJson);
                    await RetryOrAckOrRejectMessage(ea, ea.Body.ToArray(), cancellationToken: cancellationToken);
                }
            };
            await Channel.BasicConsumeAsync(QueueInfo.Name, false, consumer, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start consuming QueueName: {QueueName}", QueueInfo.Name);
        }
    }

    private async Task RetryOrAckOrRejectMessage(BasicDeliverEventArgs ea, byte[] body, int? delayMs = null, CancellationToken cancellationToken = default)
    {
        try
        {
            int currentRetryCount = 0;
            if (!UseRetry)
            {
                await Channel.BasicRejectAsync(ea.DeliveryTag, false, cancellationToken);
                return;
            }

            if (ea.BasicProperties.Headers?.TryGetValue("x-retry-count", out object? value) ?? false)
            {
                currentRetryCount = (int)value!;
            }

            if (currentRetryCount > MaxRetryCount)
            {
                Logger.LogInformation("Max retry count exceeded for message QueueName: {QueueName} Message: {Message}", QueueInfo.Name, Encoding.UTF8.GetString(body));
                await Channel.BasicRejectAsync(ea.DeliveryTag, false, cancellationToken);
                return;
            }

            await Channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);

            var delayedExchangeName = $"{QueueInfo.Name}.delayed.retry";
            delayMs ??= RetryDelayMs;
            var properties = new BasicProperties
            {
                Headers = ea.BasicProperties.Headers ?? new Dictionary<string, object?>()
            };
            properties.Headers["x-retry-count"] = currentRetryCount + 1;
            properties.Headers["x-delay"] = delayMs;
            await Channel.BasicPublishAsync(delayedExchangeName, "", true, properties, body, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to retry or ack message UseRetry: {UseRetry} QueueName: {QueueName}", UseRetry, QueueInfo.Name);
        }
    }
}