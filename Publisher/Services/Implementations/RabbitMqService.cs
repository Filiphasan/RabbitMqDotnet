using System.Text;
using System.Text.Json;
using Publisher.Models;
using Publisher.Services.Interfaces;
using RabbitMQ.Client;
using Shared.Services;

namespace Publisher.Services.Implementations;

public class RabbitMqService(RabbitMqConnectionService rabbitMqConnectionService) : IRabbitMqService
{
    public async Task SendAsync<T>(T message, string queueName, string messageId , CancellationToken cancellationToken = default)
    {
        await using var channel = await rabbitMqConnectionService.GetChannelAsync();
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?> { { "x-message-id", messageId } },
            Persistent = true,
            Priority = 0,
            DeliveryMode = DeliveryModes.Persistent
        };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        await channel.BasicPublishAsync("", queueName, false, properties, body, cancellationToken);
    }

    public async Task PublishAsync<T>(T message, string exchangeName, string routingKey, string exchangeType = ExchangeType.Direct, CancellationToken cancellationToken = default)
    {
        await using var channel = await rabbitMqConnectionService.GetChannelAsync();
        await channel.ExchangeDeclareAsync(exchangeName, exchangeType, cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        await channel.BasicPublishAsync(exchangeName, routingKey, false, body, cancellationToken: cancellationToken);
    }

    public async Task SendAsync<T>(SendMessageModel<T> message, CancellationToken cancellationToken = default) where T : class
    {
        var channel = await rabbitMqConnectionService.GetChannelAsync();
        try
        {
            var properties = new BasicProperties
            {
                Headers = new Dictionary<string, object?> { { "x-message-id", message.MessageId }, { "x-max-priority", 10 } },
                MessageId = message.MessageId,
                Priority = message.Priority,
                DeliveryMode = message.DeliveryMode,
            };
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message.Message));
            await channel.BasicPublishAsync("", message.QueueName, false, properties, body, cancellationToken);
        }
        finally
        {
            await channel.DisposeAsync();
        }
    }

    public async Task PublishAsync<T>(PublishMessageModel<T> message, CancellationToken cancellationToken = default) where T : class
    {
        var channel = await rabbitMqConnectionService.GetChannelAsync();
        try
        {
            var properties = new BasicProperties
            {
                Headers = new Dictionary<string, object?> { { "x-message-id", message.MessageId }, { "x-max-priority", 10 } },
                MessageId = message.MessageId,
                Priority = message.Priority,
                DeliveryMode = message.DeliveryMode,
            };
            await channel.ExchangeDeclareAsync(message.ExchangeName, message.ExchangeType, cancellationToken: cancellationToken);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message.Message));
            await channel.BasicPublishAsync(message.ExchangeName, message.RoutingKey, false, properties, body, cancellationToken: cancellationToken);
        }
        finally
        {
            await channel.DisposeAsync();
        }
    }
}