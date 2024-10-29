using System.Text;
using System.Text.Json;
using Publisher.Services.Interfaces;
using RabbitMQ.Client;
using Shared.Services;

namespace Publisher.Services.Implementations;

public class RabbitMqService(RabbitMqConnectionService rabbitMqConnectionService) : IRabbitMqService
{
    public async Task SendAsync<T>(T message, string queueName)
    {
        var channel = await rabbitMqConnectionService.GetChannelAsync();
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        channel.BasicPublish("", queueName, null, body);
    }

    public async Task PublishAsync<T>(T message, string exchangeName, string routingKey, string exchangeType = ExchangeType.Direct)
    {
        var channel = await rabbitMqConnectionService.GetChannelAsync();
        channel.ExchangeDeclare(exchangeName, exchangeType);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        channel.BasicPublish(exchangeName, routingKey, null, body);
    }
}