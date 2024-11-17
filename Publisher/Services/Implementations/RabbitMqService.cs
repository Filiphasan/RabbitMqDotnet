using System.Text;
using System.Text.Json;
using Publisher.Services.Interfaces;
using RabbitMQ.Client;
using Shared.Services;

namespace Publisher.Services.Implementations;

public class RabbitMqService(RabbitMqConnectionService rabbitMqConnectionService) : IRabbitMqService
{
    public async Task SendAsync<T>(T message, string queueName, string messageId, CancellationToken cancellationToken = default)
    {
        await using var channel = await rabbitMqConnectionService.GetChannelAsync();
        var properties = new BasicProperties { Headers = new Dictionary<string, object?> { { "x-message-id", messageId } } };
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
}